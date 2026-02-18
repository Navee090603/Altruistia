using NLog;

namespace Altruista834OutboundMonitor.Services
{
    public interface ILoggingService
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(string message, System.Exception ex);
    }

    public sealed class LoggingService : ILoggingService
    {
        private readonly Logger _logger;

        public LoggingService()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Info(string message) => _logger.Info(message);
        public void Warn(string message) => _logger.Warn(message);
        public void Error(string message) => _logger.Error(message);
        public void Error(string message, System.Exception ex) => _logger.Error(ex, message);
    }
}
