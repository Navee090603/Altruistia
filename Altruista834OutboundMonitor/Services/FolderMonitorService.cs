using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altruista834OutboundMonitor.Config;
using Altruista834OutboundMonitor.Models;
using Altruista834OutboundMonitor.Utilities;

namespace Altruista834OutboundMonitor.Services
{
    public sealed class FolderMonitorService
    {
        private readonly AppConfig _config;
        private readonly ILoggingService _logger;
        private readonly IEmailService _emailService;
        private readonly ISlaService _slaService;
        private readonly TimeZoneInfo _ist;
        private readonly ConcurrentDictionary<string, byte> _processedFiles = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentBag<string> _incidents = new ConcurrentBag<string>();

        public FolderMonitorService(AppConfig config, ILoggingService logger, IEmailService emailService, ISlaService slaService)
        {
            _config = config;
            _logger = logger;
            _emailService = emailService;
            _slaService = slaService;
            _ist = TimeZoneHelper.Resolve(config.Runtime.TimeZoneId);
        }

        public async Task MonitorAllAsync(CancellationToken ct)
        {
            _logger.Info("Altruista834OutboundMonitor execution started.");

            var tasks = new[]
            {
                MonitorVendorAndProprietaryAsync(ct),
                MonitorHoldAsync(ct),
                MonitorDropAsync(ct)
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
            await WriteSummaryAsync().ConfigureAwait(false);
            _logger.Info("Altruista834OutboundMonitor execution finished.");
        }

        private async Task MonitorVendorAndProprietaryAsync(CancellationToken ct)
        {
            var start = TimeSpan.Parse(_config.Monitoring.VendorWindowStart);
            var end = TimeSpan.Parse(_config.Monitoring.VendorWindowEnd);

            await MonitorWindowAsync("VendorExtractUtility", _config.Folders.VendorExtractUtility, "*.txt", start, end, async file =>
            {
                var fileName = Path.GetFileName(file.FullPath);
                if (!_config.Monitoring.ExpectedFiles.Any(exp => fileName.StartsWith(exp, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.Warn($"Unexpected vendor file ignored: {fileName}");
                    return;
                }

                await ProcessFileAsync(file, "IT-OPS", _config.Email.ItOpsRecipients, ct).ConfigureAwait(false);
                await MonitorProprietaryForFileAsync(fileName, ct).ConfigureAwait(false);
            }, _config.Email.ItOpsRecipients, "Vendor extract file missing", ct).ConfigureAwait(false);
        }

        private async Task MonitorProprietaryForFileAsync(string filename, CancellationToken ct)
        {
            var deadlineIst = TimeZoneHelper.IstNow(_ist).Date.Add(TimeSpan.Parse(_config.Monitoring.VendorWindowEnd));
            _logger.Info($"Monitoring proprietary for {filename} until {deadlineIst:yyyy-MM-dd HH:mm:ss}");
            while (TimeZoneHelper.IstNow(_ist) <= deadlineIst)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var path = Path.Combine(_config.Folders.Proprietary, filename);
                    if (File.Exists(path))
                    {
                        var metadata = FileHelper.ToMetadata(new FileInfo(path));
                        await ProcessFileAsync(metadata, "INTERNAL", _config.Email.InternalRecipients, ct).ConfigureAwait(false);

                        if (_slaService.IsBreach(DateTime.UtcNow, metadata, out var estimateEndIst, out var slaEndIst))
                        {
                            var subject = $"[INCIDENT] SLA breach risk for {filename}";
                            var body = $"Estimated completion {estimateEndIst:HH:mm} IST exceeds SLA {slaEndIst:HH:mm} IST.";
                            await _emailService.SendAsync(_config.Email.InternalRecipients, subject, body, ct).ConfigureAwait(false);
                            await _emailService.SendAsync(_config.Email.ClientRecipients, "[NOTICE] Processing delay", body, ct).ConfigureAwait(false);
                            _incidents.Add(subject + " " + body);
                        }

                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Proprietary monitoring error for {filename}", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(_config.Runtime.PollingIntervalSeconds), ct).ConfigureAwait(false);
            }

            await _emailService.SendAsync(_config.Email.InternalRecipients, "Proprietary file missing", $"File not moved to proprietary folder: {filename}", ct).ConfigureAwait(false);
            _incidents.Add("Proprietary missing: " + filename);
        }

        private async Task MonitorHoldAsync(CancellationToken ct)
        {
            var start = TimeSpan.Parse(_config.Monitoring.VendorWindowStart);
            var end = TimeSpan.Parse(_config.Monitoring.HoldWindowEnd);
            await MonitorWindowAsync("HOLD", _config.Folders.Hold, "*.*", start, end, async file =>
            {
                if (!file.IsX12)
                {
                    _logger.Warn($"HOLD file rejected (non-X12): {file.FileName}");
                    return;
                }

                if (!FileHelper.IsForToday(new FileInfo(file.FullPath), TimeZoneHelper.IstNow(_ist)))
                {
                    _logger.Warn($"HOLD file ignored (not today): {file.FileName}");
                    return;
                }

                await ProcessFileAsync(file, "INTERNAL", _config.Email.InternalRecipients, ct).ConfigureAwait(false);
            }, _config.Email.InternalRecipients, "No X12 files received in HOLD", ct).ConfigureAwait(false);
        }

        private async Task MonitorDropAsync(CancellationToken ct)
        {
            var cutoff = TimeSpan.Parse(_config.Monitoring.DropCutoff);
            var start = TimeSpan.Parse(_config.Monitoring.VendorWindowStart);

            await MonitorWindowAsync("DROP", _config.Folders.Drop, "*.*", start, cutoff, async file =>
            {
                if (!file.IsX12)
                {
                    return;
                }

                var msg = $"DROP received: Name={file.FileName}, SizeMB={file.FileSizeMb:F2}, Modified={TimeZoneHelper.UtcToIst(file.LastWriteTimeUtc, _ist):yyyy-MM-dd HH:mm:ss} IST";
                _logger.Info(msg);
                await ProcessFileAsync(file, "INTERNAL", _config.Email.InternalRecipients, ct).ConfigureAwait(false);
            }, _config.Email.InternalRecipients, "No X12 files received in DROP before cutoff", ct).ConfigureAwait(false);
        }

        private async Task MonitorWindowAsync(string stepName, string folder, string pattern, TimeSpan start, TimeSpan end, Func<FileMetadata, Task> onFile, IEnumerable<string> missedRecipients, string missedSubject, CancellationToken ct)
        {
            var nowIst = TimeZoneHelper.IstNow(_ist);
            var startIst = nowIst.Date.Add(start);
            var endIst = nowIst.Date.Add(end);

            if (nowIst < startIst)
            {
                var wait = startIst - nowIst;
                _logger.Info($"{stepName}: waiting for window start ({wait}).");
                await Task.Delay(wait, ct).ConfigureAwait(false);
            }

            using (var watcher = BuildWatcher(folder, pattern))
            {
                watcher.EnableRaisingEvents = true;
                while (TimeZoneHelper.IstNow(_ist) <= endIst)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        foreach (var fileInfo in FileHelper.SafeEnumerate(folder, pattern))
                        {
                            var key = fileInfo.FullName;
                            if (_processedFiles.ContainsKey(key))
                            {
                                continue;
                            }

                            if ((DateTime.UtcNow - fileInfo.LastWriteTimeUtc).TotalMinutes > _config.Monitoring.StaleFileMinutes)
                            {
                                await _emailService.SendAsync(missedRecipients, $"Stale file in {stepName}", $"{fileInfo.Name} has been idle for > {_config.Monitoring.StaleFileMinutes} minutes", ct).ConfigureAwait(false);
                            }

                            var isStable = await FileHelper.WaitForFileStableAsync(fileInfo.FullName, _config.Runtime.PartialFileStableSeconds, ct).ConfigureAwait(false);
                            if (!isStable || !FileHelper.CanOpenRead(fileInfo.FullName))
                            {
                                _logger.Warn($"File still unstable or locked: {fileInfo.FullName}");
                                continue;
                            }

                            if (_processedFiles.TryAdd(key, 0))
                            {
                                await onFile(FileHelper.ToMetadata(fileInfo)).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (IOException ioEx)
                    {
                        _logger.Error($"I/O monitoring issue ({stepName}).", ioEx);
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        _logger.Error($"Access denied monitoring issue ({stepName}).", uaEx);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Unexpected monitoring issue ({stepName}).", ex);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_config.Runtime.PollingIntervalSeconds), ct).ConfigureAwait(false);
                }
            }

            var foundInWindow = FileHelper.SafeEnumerate(folder, pattern).Any(f => f.LastWriteTime.Date == DateTime.Today);
            if (!foundInWindow)
            {
                await _emailService.SendAsync(missedRecipients, missedSubject, $"No eligible files arrived in {stepName} during configured window.", ct).ConfigureAwait(false);
                _incidents.Add(missedSubject);
            }
        }

        private FileSystemWatcher BuildWatcher(string path, string pattern)
        {
            var watcher = new FileSystemWatcher(path)
            {
                Filter = pattern,
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
            };

            watcher.Error += (_, args) => _logger.Error($"FileSystemWatcher error ({path})", args.GetException());
            watcher.Created += (_, args) => _logger.Info($"Detected created file: {args.FullPath}");
            watcher.Changed += (_, args) => _logger.Info($"Detected changed file: {args.FullPath}");
            return watcher;
        }

        private async Task ProcessFileAsync(FileMetadata file, string team, IEnumerable<string> recipients, CancellationToken ct)
        {
            await Task.Yield();
            _logger.Info($"Processing [{team}] file {file.FileName} ({file.FileSizeMb:F2} MB).");
            if ((DateTime.UtcNow - file.LastWriteTimeUtc).TotalMinutes > _config.Monitoring.StaleFileMinutes)
            {
                await _emailService.SendAsync(recipients, $"Stale file detected: {file.FileName}", $"File remained in folder over {_config.Monitoring.StaleFileMinutes} minutes.", ct).ConfigureAwait(false);
            }
        }

        private async Task WriteSummaryAsync()
        {
            var outputFolder = _config.Folders.ReportOutput;
            Directory.CreateDirectory(outputFolder);
            var now = DateTime.UtcNow;
            var summaryPath = Path.Combine(outputFolder, $"ExecutionSummary_{now:yyyyMMdd}.log");
            var incidentPath = Path.Combine(outputFolder, $"IncidentReport_{now:yyyyMMdd}.log");

            var summary = new StringBuilder();
            summary.AppendLine($"Execution ended UTC: {now:O}");
            summary.AppendLine($"Processed files count: {_processedFiles.Count}");
            summary.AppendLine($"Incident count: {_incidents.Count}");

            await File.WriteAllTextAsync(summaryPath, summary.ToString()).ConfigureAwait(false);
            await File.WriteAllLinesAsync(incidentPath, _incidents.DefaultIfEmpty("No incidents.")).ConfigureAwait(false);
        }
    }
}
