using System;
using Altruista834OutboundMonitor.Config;
using Altruista834OutboundMonitor.Models;
using Altruista834OutboundMonitor.Utilities;

namespace Altruista834OutboundMonitor.Services
{
    public interface ISlaService
    {
        bool IsBreach(DateTime processingStartUtc, FileMetadata file, out DateTime estimateEndIst, out DateTime slaEndIst);
    }

    public sealed class SlaService : ISlaService
    {
        private readonly AppConfig _config;
        private readonly TimeZoneInfo _ist;

        public SlaService(AppConfig config)
        {
            _config = config;
            _ist = TimeZoneHelper.Resolve(config.Runtime.TimeZoneId);
        }

        public bool IsBreach(DateTime processingStartUtc, FileMetadata file, out DateTime estimateEndIst, out DateTime slaEndIst)
        {
            var startIst = TimeZoneHelper.UtcToIst(processingStartUtc, _ist);
            estimateEndIst = _config.SLA.EstimateCompletionIst(startIst, file.FileSizeBytes);
            slaEndIst = startIst.Date.Add(_config.SLA.SLAEndIst);
            return estimateEndIst > slaEndIst;
        }
    }
}
