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
    /// 如果你想让策划表更好读，也可以这样：
    /// - 第 1 行：英文代码字段名
    /// - 第 2 行：中文说明
    /// - 第 3 行：字段类型
    /// - 第 4 行开始：数据
    ///
    /// 这种情况读取时用：
    /// ExcelTableLoader.LoadSheet<YourRow>(path, "礼物表", 1, 4)
    ///
    /// 规则：
    /// - 类名建议和 sheet 名对应
    /// - 字段名和表头尽量保持一致
    /// - 字段类型支持 string / int / float / bool / enum
    /// - 如果只是想看整张表，不想转成类，用 LoadRawSheet
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
        /// 读取整个 Excel 文件里的所有 sheet。
        /// 不做反射，不转成类，直接返回原始表格内容。
        ///
        /// 返回值：
        /// sheet 名 -> 表格二维列表
        ///
        /// 适合你现在这种“先看看整张表读出来是什么样”的阶段。
        /// </summary>
        public static Dictionary<string, List<List<string>>> LoadAllRawSheets(string filePath)
        {
            var result = new Dictionary<string, List<List<string>>>();
            using var workbook = new XlsxWorkbook(filePath);
            foreach (var sheetName in workbook.SheetNames)
            {
                result[sheetName] = workbook.ReadSheet(sheetName);
            }

            return result;
        }

        /// <summary>
        /// 读取一整张 sheet。
        /// 不跳过任何行，不转成类。
        ///
        /// 例如礼物表会完整读出：
        /// - 第 1 行：英文代码字段名
        /// - 第 2 行：中文说明
        /// - 第 3 行：字段类型
        /// - 第 4 行开始：礼物数据
        /// </summary>
        public static List<List<string>> LoadRawSheet(string filePath, string sheetName)
        {
            using var workbook = new XlsxWorkbook(filePath);
            return workbook.ReadSheet(sheetName);
        }

        /// <summary>
        /// 读取单个 sheet。
        /// 默认第 1 行是表头，第 2 行开始是数据。
        /// </summary>
        public static List<T> LoadSheet<T>(string filePath, string sheetName) where T : new()
        {
            return LoadSheet<T>(filePath, sheetName, 1, 2);
        }

        /// <summary>
        /// 读取单个 sheet。
        ///
        /// headerRow：表头在第几行，从 1 开始数
        /// dataStartRow：数据从第几行开始，从 1 开始数
        ///
        /// 例子：
        /// - 第 1 行是英文表头
        /// - 第 2 行是中文说明
        /// - 第 3 行是类型
        /// - 第 4 行开始是数据
        ///
        /// 调用：
        /// LoadSheet<GiftConfigRow>(path, "礼物表", 1, 4)
        /// </summary>
        public static List<T> LoadSheet<T>(string filePath, string sheetName, int headerRow, int dataStartRow) where T : new()
        {
            using var workbook = new XlsxWorkbook(filePath);
            return LoadSheet<T>(workbook, sheetName, headerRow, dataStartRow);
        }

        private static List<T> LoadSheet<T>(XlsxWorkbook workbook, string sheetName) where T : new()
        {
            return LoadSheet<T>(workbook, sheetName, 1, 2);
        }

        private static List<T> LoadSheet<T>(XlsxWorkbook workbook, string sheetName, int headerRow, int dataStartRow) where T : new()
        {
            var rows = workbook.ReadSheet(sheetName);
            if (rows.Count == 0)
            {
                return new List<T>();
            }

            var headerIndex = Math.Max(0, headerRow - 1);
            var dataStartIndex = Math.Max(headerIndex + 1, dataStartRow - 1);
            var headerCells = rows[headerIndex];
            var headers = headerCells.Select(x => x?.Trim() ?? string.Empty).ToArray();
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            var fieldMap = fields.ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

            var list = new List<T>();
            for (var rowIndex = dataStartIndex; rowIndex < rows.Count; rowIndex++)
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
