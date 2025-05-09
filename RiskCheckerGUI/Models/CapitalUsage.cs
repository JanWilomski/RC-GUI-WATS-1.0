namespace RiskCheckerGUI.Models
{
    public class CapitalUsage
    {
        public double OpenCapital { get; set; }
        public double AccruedCapital { get; set; }
        public double TotalCapital { get; set; }
        
        // Limity i ich wykorzystanie
        public int MessageLimit { get; set; } = 115200; // Domyślny limit
        public int UsedMessages { get; set; }
        public double MessageUsagePercent => (double)UsedMessages / MessageLimit * 100;
        
        public double CapitalLimit { get; set; } = 100000; // Domyślny limit
        public double UsedCapital { get; set; }
        public double CapitalUsagePercent => UsedCapital / CapitalLimit * 100;
    }
}