namespace RiskCheckerGUI.Models
{
    public class InstrumentPosition
    {
        public string ISIN { get; set; }
        public string TickerName { get; set; }
        public int Net { get; set; }
        public int OpenLong { get; set; }
        public int OpenShort { get; set; }
    }
}