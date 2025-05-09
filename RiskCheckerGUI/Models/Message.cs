using System;

namespace RiskCheckerGUI.Models
{
    public enum LogType
    {
        Debug = 'D',
        Info = 'I',
        Warning = 'W',
        Error = 'E'
    }

    public class LogMessage
    {
        public LogType Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}