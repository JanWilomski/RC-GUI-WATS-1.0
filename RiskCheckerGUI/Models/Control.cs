namespace RiskCheckerGUI.Models
{
    public enum ControlType
    {
        Halt,
        MaxOrderRate,
        MaxTransaction,
        MaxAbsShares,
        MaxShortShares
    }

    public class Control
    {
        public string Scope { get; set; }
        public ControlType ControlName { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return $"{Scope},{ControlName},{Value}";
        }

        public static Control Parse(string controlString)
        {
            var parts = controlString.Split(',');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid control string format");

            return new Control
            {
                Scope = parts[0],
                ControlName = Enum.Parse<ControlType>(parts[1], true),
                Value = parts[2]
            };
        }
    }
}