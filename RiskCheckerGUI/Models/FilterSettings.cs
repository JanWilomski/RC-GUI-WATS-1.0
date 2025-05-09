namespace RiskCheckerGUI.Models
{
    public class FilterSettings
    {
        public string Name { get; set; }
        public string MessageTypeFilter { get; set; }
        public string SymbolFilter { get; set; }
        public string IsinFilter { get; set; }
        public bool ShowDebugMessages { get; set; }
        public bool ShowInfoMessages { get; set; }
        public bool ShowWarningMessages { get; set; }
        public bool ShowErrorMessages { get; set; }
    }
}