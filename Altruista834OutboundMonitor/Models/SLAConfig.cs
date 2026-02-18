using System;

namespace Altruista834OutboundMonitor.Models
{
    public sealed class SLAConfig
    {
        public long LargeFileThresholdMb { get; set; }
        public double ProcessingHoursPerGb { get; set; }
        public TimeSpan SLAEndIst { get; set; }

        public DateTime EstimateCompletionIst(DateTime startIst, long sizeBytes)
        {
            var sizeGb = sizeBytes / 1024d / 1024d / 1024d;
            var hours = Math.Max(0.01d, sizeGb * ProcessingHoursPerGb);
            return startIst.AddHours(hours);
        }
    }
}
