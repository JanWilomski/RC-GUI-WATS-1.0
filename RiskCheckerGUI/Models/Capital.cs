namespace RiskCheckerGUI.Models
{
    public class Capital
    {
        public double OpenCapital { get; set; }
        public double AccruedCapital { get; set; }
        public double TotalCapital { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}