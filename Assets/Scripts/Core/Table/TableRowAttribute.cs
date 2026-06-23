using System;

namespace Core.Table
{
    /// <summary>
    /// 标记一个类是“表格的一行数据”。
    ///
    /// 读取规则很简单：
    /// - Excel 第一行是表头
    /// - 表头名字要和类里的字段名一致
    /// - 每一行会被反射成一个对象
    ///
    /// 例子：
    /// 表头：ID | Name | Value
    /// 类里：public string ID; public string Name; public int Value;
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TableRowAttribute : Attribute
    {
    }
}
