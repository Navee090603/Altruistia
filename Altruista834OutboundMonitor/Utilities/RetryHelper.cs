using System;
using System.Threading;
using System.Threading.Tasks;

namespace Altruista834OutboundMonitor.Utilities
{
    public static class RetryHelper
    {
        public static async Task RunAsync(Func<Task> action, int retries, TimeSpan delay, Action<Exception, int>? onError, CancellationToken ct)
        {
            for (var attempt = 1; attempt <= retries; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await action().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (attempt < retries)
                {
                    onError?.Invoke(ex, attempt);
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }

            await action().ConfigureAwait(false);
        }
    }
}
