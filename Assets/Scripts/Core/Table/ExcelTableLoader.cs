using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace Core.Table
{
    /// <summary>
    /// 超轻量 Excel 读取器。
    ///
    /// 说明：
    /// - 只面向 .xlsx
    /// - 通过反射，把表格每一行映射成一个类
    /// - 适合比赛项目，不追求大而全
    ///
    /// 表结构建议写法：
    ///
    /// 1) 第一行：字段名
    ///    例如：ID | Name | Type | InitValue | MinValue | MaxValue | Desc
    ///
    /// 2) 第二行开始：数据
    ///    例如：A_1 | 支持率 | float | 50 | 0 | 100 | 开局支持率
    ///
    /// 规则：
    /// - 类名建议和 sheet 名对应
    /// - 字段名和表头尽量保持一致
    /// - 字段类型支持 string / int / float / bool / enum
    /// - 如果你想写类型说明，建议另开说明文档，不要放在数据表第二行
    /// </summary>
    public static class ExcelTableLoader
    {
        /// <summary>
        /// 读取一个 xlsx 文件中的所有 sheet。
        /// 返回值：sheet 名 -> 该 sheet 对应的行对象列表。
        /// </summary>
        public static Dictionary<string, List<T>> LoadAllSheets<T>(string filePath) where T : new()
        {
            var result = new Dictionary<string, List<T>>();
            using var workbook = new XlsxWorkbook(filePath);
            foreach (var sheetName in workbook.SheetNames)
            {
                result[sheetName] = LoadSheet<T>(workbook, sheetName);
            }

            return result;
        }

        /// <summary>
        /// 读取单个 sheet。
        /// 默认第 1 行是表头，第 2 行开始是数据。
        /// </summary>
        public static List<T> LoadSheet<T>(string filePath, string sheetName) where T : new()
        {
            using var workbook = new XlsxWorkbook(filePath);
            return LoadSheet<T>(workbook, sheetName);
        }

        private static List<T> LoadSheet<T>(XlsxWorkbook workbook, string sheetName) where T : new()
        {
            var rows = workbook.ReadSheet(sheetName);
            if (rows.Count == 0)
            {
                return new List<T>();
            }

            var headerRow = rows[0];
            var headers = headerRow.Select(x => x?.Trim() ?? string.Empty).ToArray();
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            var fieldMap = fields.ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

            var list = new List<T>();
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var item = new T();

                for (var colIndex = 0; colIndex < headers.Length && colIndex < row.Count; colIndex++)
                {
                    var header = headers[colIndex];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    if (!fieldMap.TryGetValue(header, out var field))
                    {
                        continue;
                    }

                    var raw = row[colIndex];
                    var value = ConvertValue(raw, field.FieldType);
                    field.SetValue(item, value);
                }

                list.Add(item);
            }

            return list;
        }

        private static object ConvertValue(string raw, Type targetType)
        {
            if (targetType == typeof(string))
            {
                return raw ?? string.Empty;
            }

            if (targetType == typeof(int))
            {
                return int.TryParse(raw, out var value) ? value : 0;
            }

            if (targetType == typeof(float))
            {
                return float.TryParse(raw, out var value) ? value : 0f;
            }

            if (targetType == typeof(bool))
            {
                if (bool.TryParse(raw, out var value))
                {
                    return value;
                }

                return raw == "1" || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
            }

            if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, raw, true);
                }
                catch
                {
                    return Activator.CreateInstance(targetType);
                }
            }

            return null;
        }

        /// <summary>
        /// 轻量 xlsx 读取器。
        /// 只实现我们这次需要的最小功能。
        /// </summary>
        private sealed class XlsxWorkbook : IDisposable
        {
            private readonly ZipArchive _archive;
            private readonly List<string> _sharedStrings;
            private readonly Dictionary<string, string> _sheetTargets;

            public XlsxWorkbook(string filePath)
            {
                _archive = ZipFile.OpenRead(filePath);
                _sharedStrings = LoadSharedStrings();
                _sheetTargets = LoadSheetTargets();
            }

            public IEnumerable<string> SheetNames => _sheetTargets.Keys;

            public List<List<string>> ReadSheet(string sheetName)
            {
                if (!_sheetTargets.TryGetValue(sheetName, out var targetPath))
                {
                    return new List<List<string>>();
                }

                var entry = _archive.GetEntry(targetPath);
                if (entry == null)
                {
                    return new List<List<string>>();
                }

                using var stream = entry.Open();
                var xdoc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                var sheetData = xdoc.Root?.Element(ns + "sheetData");
                var result = new List<List<string>>();
                if (sheetData == null)
                {
                    return result;
                }

                foreach (var row in sheetData.Elements(ns + "row"))
                {
                    var cells = new List<string>();
                    foreach (var cell in row.Elements(ns + "c"))
                    {
                        var cellRef = cell.Attribute("r")?.Value ?? string.Empty;
                        var index = GetColumnIndex(cellRef);
                        while (cells.Count <= index)
                        {
                            cells.Add(string.Empty);
                        }

                        cells[index] = ReadCellValue(cell, ns);
                    }

                    result.Add(cells);
                }

                return result;
            }

            private string ReadCellValue(XElement cell, XNamespace ns)
            {
                var cellType = cell.Attribute("t")?.Value;
                var valueElement = cell.Element(ns + "v");
                var value = valueElement?.Value ?? string.Empty;

                if (cellType == "s")
                {
                    if (int.TryParse(value, out var index) && index >= 0 && index < _sharedStrings.Count)
                    {
                        return _sharedStrings[index];
                    }
                }

                return value;
            }

            private List<string> LoadSharedStrings()
            {
                var list = new List<string>();
                var entry = _archive.GetEntry("xl/sharedStrings.xml");
                if (entry == null)
                {
                    return list;
                }

                using var stream = entry.Open();
                var xdoc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                var strings = xdoc.Root?.Elements(ns + "si").ToList() ?? new List<XElement>();
                for (var i = 0; i < strings.Count; i++)
                {
                    list.Add(GetSharedStringText(strings[i], ns));
                }

                return list;
            }

            private static string GetSharedStringText(XElement node, XNamespace ns)
            {
                var textNode = node.Descendants(ns + "t").FirstOrDefault();
                return textNode?.Value ?? string.Empty;
            }

            private Dictionary<string, string> LoadSheetTargets()
            {
                var dict = new Dictionary<string, string>();
                var workbookEntry = _archive.GetEntry("xl/workbook.xml");
                var relEntry = _archive.GetEntry("xl/_rels/workbook.xml.rels");
                if (workbookEntry == null || relEntry == null)
                {
                    return dict;
                }

                var relMap = LoadRelationships(relEntry);
                using var stream = workbookEntry.Open();
                var xdoc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

                foreach (var sheet in xdoc.Root?.Element(ns + "sheets")?.Elements(ns + "sheet") ?? Enumerable.Empty<XElement>())
                {
                    var name = sheet.Attribute("name")?.Value;
                    var relId = sheet.Attribute(relNs + "id")?.Value;
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relId))
                    {
                        continue;
                    }

                    if (!relMap.TryGetValue(relId, out var target))
                    {
                        continue;
                    }

                    dict[name] = target;
                }

                return dict;
            }

            private static Dictionary<string, string> LoadRelationships(ZipArchiveEntry relEntry)
            {
                var dict = new Dictionary<string, string>();
                using var stream = relEntry.Open();
                var xdoc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/package/2006/relationships";

                foreach (var rel in xdoc.Root?.Elements(ns + "Relationship") ?? Enumerable.Empty<XElement>())
                {
                    var id = rel.Attribute("Id")?.Value;
                    var target = rel.Attribute("Target")?.Value;
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(target))
                    {
                        continue;
                    }

                    dict[id] = "xl/" + target.Replace("\\", "/").TrimStart('/');
                }

                return dict;
            }

            private static int GetColumnIndex(string cellReference)
            {
                var letters = new StringBuilder();
                for (var i = 0; i < cellReference.Length; i++)
                {
                    if (char.IsLetter(cellReference[i]))
                    {
                        letters.Append(cellReference[i]);
                    }
                }

                var result = 0;
                for (var i = 0; i < letters.Length; i++)
                {
                    result *= 26;
                    result += char.ToUpperInvariant(letters[i]) - 'A' + 1;
                }

                return Math.Max(0, result - 1);
            }

            public void Dispose()
            {
                _archive.Dispose();
            }
        }
    }
}
