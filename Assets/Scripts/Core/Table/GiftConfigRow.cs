namespace Core.Table
{
    /// <summary>
    /// 礼物表的一行数据。
    /// 字段名必须和 Excel 第一行表头一致。
    /// </summary>
    public class GiftConfigRow
    {
        public int ID;
        public string Variable;
        public string Name;
        public int SupportRateDelta;
        public int HeartRateDelta;
        public string Desc;
    }
}
