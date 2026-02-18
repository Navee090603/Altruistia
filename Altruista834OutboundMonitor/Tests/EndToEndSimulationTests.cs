using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Altruista834OutboundMonitor.Config;
using Altruista834OutboundMonitor.Services;
using NUnit.Framework;

namespace Altruista834OutboundMonitor.Tests
{
    [TestFixture]
    public class EndToEndSimulationTests
    {
        [Test]
        public async Task Should_Run_With_FileArrival_And_EmailTriggers()
        {
            using (var helper = new FolderSimulationHelper())
            {
                var sentEmails = new ConcurrentBag<string>();
                var appConfig = BuildConfig(helper);
                var logger = new TestLogger();
                var email = new MockEmailService(sentEmails);
                var sla = new SlaService(appConfig);
                var monitor = new FolderMonitorService(appConfig, logger, email, sla);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(600).ConfigureAwait(false);
                        await helper.CreateFileAsync(helper.Vendor, "C_today.txt", partialWrite: true).ConfigureAwait(false);
                        await helper.CreateFileAsync(helper.Proprietary, "C_today.txt", sizeKb: 1000).ConfigureAwait(false);
                        await helper.CreateFileAsync(helper.Hold, "TX_X12.edi").ConfigureAwait(false);
                        await helper.CreateFileAsync(helper.Drop, "TX_DROP_X12.edi", lockFile: true).ConfigureAwait(false);
                    });

                    try { await monitor.MonitorAllAsync(cts.Token).ConfigureAwait(false); } catch (OperationCanceledException) { }
                }

                Assert.That(logger.InfoCount, Is.GreaterThan(0));
                Assert.That(sentEmails.Count, Is.GreaterThanOrEqualTo(0));
                Assert.That(File.Exists(Path.Combine(helper.Reports, $"ExecutionSummary_{DateTime.UtcNow:yyyyMMdd}.log")), Is.True);
            }
        }

        [Test]
        public async Task Should_Trigger_Missing_File_Alerts_When_NoFilesArrive()
        {
            using (var helper = new FolderSimulationHelper())
            {
                var sentEmails = new ConcurrentBag<string>();
                var appConfig = BuildConfig(helper);
                var logger = new TestLogger();
                var email = new MockEmailService(sentEmails);
                var sla = new SlaService(appConfig);
                var monitor = new FolderMonitorService(appConfig, logger, email, sla);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4)))
                {
                    try { await monitor.MonitorAllAsync(cts.Token).ConfigureAwait(false); } catch (OperationCanceledException) { }
                }

                Assert.That(sentEmails.Count, Is.GreaterThan(0));
            }
        }

        private static AppConfig BuildConfig(FolderSimulationHelper helper)
        {
            var now = DateTime.Now;
            return new AppConfig
            {
                Folders = new FolderConfig
                {
                    VendorExtractUtility = helper.Vendor,
                    Proprietary = helper.Proprietary,
                    Hold = helper.Hold,
                    Drop = helper.Drop,
                    ReportOutput = helper.Reports
                },
                Runtime = new RuntimeConfig
                {
                    PollingIntervalSeconds = 1,
                    PartialFileStableSeconds = 2,
                    RetryCount = 2,
                    RetryDelayMilliseconds = 100,
                    TimeZoneId = "Asia/Kolkata"
                },
                Monitoring = new MonitoringConfig
                {
                    ExpectedFileCount = 2,
                    ExpectedFiles = new List<string> { "C", "Pend_C" },
                    VendorWindowStart = now.AddSeconds(-1).ToString("HH:mm"),
                    VendorWindowEnd = now.AddMinutes(2).ToString("HH:mm"),
                    HoldWindowEnd = now.AddMinutes(2).ToString("HH:mm"),
                    DropCutoff = now.AddMinutes(2).ToString("HH:mm"),
                    StaleFileMinutes = 1
                },
                Email = new EmailConfig
                {
                    Sender = "test@local",
                    RetryCount = 1,
                    RetryDelaySeconds = 1,
                    ItOpsRecipients = new List<string> { "itops@test" },
                    InternalRecipients = new List<string> { "internal@test" },
                    ClientRecipients = new List<string> { "client@test" }
                },
                SLA = new Models.SLAConfig
                {
                    LargeFileThresholdMb = 700,
                    ProcessingHoursPerGb = 4.3,
                    SLAEndIst = new TimeSpan(10, 0, 0)
                }
            };
        }

        private sealed class MockEmailService : IEmailService
        {
            private readonly ConcurrentBag<string> _sentEmails;
            public MockEmailService(ConcurrentBag<string> sentEmails) => _sentEmails = sentEmails;
            public Task SendAsync(IEnumerable<string> recipients, string subject, string body, CancellationToken ct)
            {
                _sentEmails.Add(subject + "|" + body);
                return Task.CompletedTask;
            }
        }

        private sealed class TestLogger : ILoggingService
        {
            public int InfoCount { get; private set; }
            public void Info(string message) => InfoCount++;
            public void Warn(string message) { }
            public void Error(string message) { }
            public void Error(string message, Exception ex) { }
        }
    }
}
