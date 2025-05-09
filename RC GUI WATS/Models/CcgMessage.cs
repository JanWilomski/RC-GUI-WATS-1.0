using System;

namespace RiskCheckerGUI.Models
{
    public class CcgMessage
    {
        public string Header { get; set; }
        public string Type { get; set; }
        public int MsgSeqNum { get; set; }
        public DateTime DateReceived { get; set; }
        public string TransactTime { get; set; }
        public decimal Price { get; set; }
        public string Side { get; set; }
        public string Symbol { get; set; }
        public string OrderID { get; set; }
        
        // Raw message data
        public byte[] RawData { get; set; }
    }
}