using System;
using System.Collections.Generic;

namespace Altruista834OutboundMonitor.Models
{
    public sealed class MonitoringRule
    {
        public string RuleName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public int ExpectedFileCount { get; set; }
        public List<string> ExpectedFilePrefixes { get; set; } = new List<string>();
        public string FilePattern { get; set; } = "*.txt";
        public TimeSpan WindowStartIst { get; set; }
        public TimeSpan WindowEndIst { get; set; }
        public int StaleMinutes { get; set; } = 10;
        public bool X12Only { get; set; }
    }
}
