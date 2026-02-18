using System;

namespace Altruista834OutboundMonitor.Models
{
    public sealed class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public DateTime FirstSeenUtc { get; set; }
        public bool IsX12 { get; set; }

        public double FileSizeMb => FileSizeBytes / 1024d / 1024d;
    }
}
