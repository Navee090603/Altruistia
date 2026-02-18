using System.Collections.Generic;
using Altruista834OutboundMonitor.Models;

namespace Altruista834OutboundMonitor.Config
{
    public sealed class AppConfig
    {
        public FolderConfig Folders { get; set; } = new FolderConfig();
        public EmailConfig Email { get; set; } = new EmailConfig();
        public RuntimeConfig Runtime { get; set; } = new RuntimeConfig();
        public SLAConfig SLA { get; set; } = new SLAConfig();
        public MonitoringConfig Monitoring { get; set; } = new MonitoringConfig();
    }

    public sealed class FolderConfig
    {
        public string VendorExtractUtility { get; set; } = string.Empty;
        public string Proprietary { get; set; } = string.Empty;
        public string Hold { get; set; } = string.Empty;
        public string Drop { get; set; } = string.Empty;
        public string ReportOutput { get; set; } = string.Empty;
    }

    public sealed class EmailConfig
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 25;
        public bool EnableSsl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public List<string> ItOpsRecipients { get; set; } = new List<string>();
        public List<string> InternalRecipients { get; set; } = new List<string>();
        public List<string> ClientRecipients { get; set; } = new List<string>();
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 15;
    }

    public sealed class RuntimeConfig
    {
        public int PollingIntervalSeconds { get; set; } = 20;
        public int RetryCount { get; set; } = 5;
        public int RetryDelayMilliseconds { get; set; } = 2000;
        public int PartialFileStableSeconds { get; set; } = 20;
        public string TimeZoneId { get; set; } = "India Standard Time";
    }

    public sealed class MonitoringConfig
    {
        public int ExpectedFileCount { get; set; } = 2;
        public List<string> ExpectedFiles { get; set; } = new List<string>();
        public string VendorWindowStart { get; set; } = "06:00";
        public string VendorWindowEnd { get; set; } = "08:30";
        public string HoldWindowEnd { get; set; } = "10:00";
        public string DropCutoff { get; set; } = "09:55";
        public int StaleFileMinutes { get; set; } = 10;
    }
}
