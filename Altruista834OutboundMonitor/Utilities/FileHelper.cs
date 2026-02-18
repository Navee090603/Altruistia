using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Altruista834OutboundMonitor.Models;

namespace Altruista834OutboundMonitor.Utilities
{
    public static class FileHelper
    {
        public static FileMetadata ToMetadata(FileInfo file)
        {
            return new FileMetadata
            {
                FileName = file.Name,
                FullPath = file.FullName,
                FileSizeBytes = file.Length,
                LastWriteTimeUtc = file.LastWriteTimeUtc,
                FirstSeenUtc = DateTime.UtcNow,
                IsX12 = IsX12File(file.FullName)
            };
        }

        public static bool IsX12File(string path)
        {
            var ext = Path.GetExtension(path);
            return string.Equals(ext, ".x12", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".edi", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(path).Contains("X12", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsForToday(FileInfo file, DateTime nowIst)
        {
            return file.LastWriteTime.Date == nowIst.Date;
        }

        public static async Task<bool> WaitForFileStableAsync(string path, int stableSeconds, CancellationToken ct)
        {
            long? previous = null;
            var probes = Math.Max(2, stableSeconds / 2);
            for (var i = 0; i < probes; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (!File.Exists(path))
                {
                    return false;
                }

                var size = new FileInfo(path).Length;
                if (previous.HasValue && size == previous.Value)
                {
                    return true;
                }

                previous = size;
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }

            return false;
        }

        public static bool CanOpenRead(string path)
        {
            try
            {
                using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static FileInfo[] SafeEnumerate(string folder, string pattern)
        {
            if (!Directory.Exists(folder))
            {
                return Array.Empty<FileInfo>();
            }

            return new DirectoryInfo(folder).GetFiles(pattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToArray();
        }
    }
}
