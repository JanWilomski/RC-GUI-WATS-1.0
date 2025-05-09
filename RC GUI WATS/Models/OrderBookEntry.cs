using System;

namespace RiskCheckerGUI.Models
{
    public class OrderBookEntry
    {
        public string OrderID { get; set; }
        public string TransactTime { get; set; }
        public string Side { get; set; }
        public string Ticker { get; set; }
        public decimal Price { get; set; }
        public int OrderQty { get; set; }
        public int CumQty { get; set; }
        public int LeavesQty { get; set; }
        public string MarketID { get; set; }
        public string Account { get; set; }
        public string LastModified { get; set; }
        public string OrigOrderID { get; set; }
        public string Text { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
