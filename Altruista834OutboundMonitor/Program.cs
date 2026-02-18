using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Altruista834OutboundMonitor.Config;
using Altruista834OutboundMonitor.Services;

namespace Altruista834OutboundMonitor
{
    internal static class Program
    {
        private static int _cancelAttempts;

        private static async Task<int> Main(string[] args)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var logger = new LoggingService();
            logger.Info("Application bootstrapping started.");

            try
            {
                var config = ConfigManager.Load(basePath);
                EnsureFolders(config);

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, eventArgs) =>
                    {
                        _cancelAttempts++;
                        if (_cancelAttempts == 1)
                        {
                            eventArgs.Cancel = true;
                            logger.Warn("Cancel request ignored once to prevent accidental interruption. Press Ctrl+C again to force shutdown.");
                            return;
                        }

                        logger.Warn("Forced cancellation requested.");
                        cts.Cancel();
                    };

                    var emailService = new EmailService(config.Email, logger);
                    var slaService = new SlaService(config);
                    var monitorService = new FolderMonitorService(config, logger, emailService, slaService);

                    await monitorService.MonitorAllAsync(cts.Token).ConfigureAwait(false);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                logger.Warn("Application cancelled.");
                return 2;
            }
            catch (Exception ex)
            {
                logger.Error("Fatal application error.", ex);
                return 1;
            }
            finally
            {
                logger.Info("Application shutdown complete.");
            }
        }

        private static void EnsureFolders(AppConfig config)
        {
            Directory.CreateDirectory(config.Folders.VendorExtractUtility);
            Directory.CreateDirectory(config.Folders.Proprietary);
            Directory.CreateDirectory(config.Folders.Hold);
            Directory.CreateDirectory(config.Folders.Drop);
            Directory.CreateDirectory(config.Folders.ReportOutput);
        }
    }
}
