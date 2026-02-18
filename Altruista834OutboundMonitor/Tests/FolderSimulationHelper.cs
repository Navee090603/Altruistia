using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Altruista834OutboundMonitor.Tests
{
    public sealed class FolderSimulationHelper : IDisposable
    {
        public string Root { get; }
        public string Vendor { get; }
        public string Proprietary { get; }
        public string Hold { get; }
        public string Drop { get; }
        public string Reports { get; }

        public FolderSimulationHelper()
        {
            Root = Path.Combine(Path.GetTempPath(), "Altruista834Tests", Guid.NewGuid().ToString("N"));
            Vendor = Directory.CreateDirectory(Path.Combine(Root, "VendorExtractUtility")).FullName;
            Proprietary = Directory.CreateDirectory(Path.Combine(Vendor, "Proprietary")).FullName;
            Hold = Directory.CreateDirectory(Path.Combine(Root, "HOLD")).FullName;
            Drop = Directory.CreateDirectory(Path.Combine(Root, "DROP")).FullName;
            Reports = Directory.CreateDirectory(Path.Combine(Root, "Reports")).FullName;
        }

        public async Task<string> CreateFileAsync(string folder, string name, int sizeKb = 4, bool partialWrite = false, bool lockFile = false)
        {
            var path = Path.Combine(folder, name);
            var content = Encoding.ASCII.GetBytes(new string('A', sizeKb * 1024));

            if (partialWrite)
            {
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await stream.WriteAsync(content, 0, content.Length / 2).ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                    await stream.WriteAsync(content, content.Length / 2, content.Length - (content.Length / 2)).ConfigureAwait(false);
                }
            }
            else
            {
                await File.WriteAllBytesAsync(path, content).ConfigureAwait(false);
            }

            if (lockFile)
            {
                _ = Task.Run(() =>
                {
                    using (var lockStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        Thread.Sleep(500);
                    }
                });
            }

            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
