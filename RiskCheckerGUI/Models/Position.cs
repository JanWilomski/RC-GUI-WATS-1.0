namespace RiskCheckerGUI.Models
{
    public class Position
    {
        public string ISIN { get; set; }
        public int Net { get; set; }
        public int OpenLong { get; set; }
        public int OpenShort { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}