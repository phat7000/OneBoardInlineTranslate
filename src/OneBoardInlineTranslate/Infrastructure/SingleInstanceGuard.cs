namespace OneBoardInlineTranslate.Infrastructure;

internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex? _mutex;

    private SingleInstanceGuard(Mutex? mutex, bool ownsInstance)
    {
        _mutex = mutex;
        OwnsInstance = ownsInstance;
    }

    internal bool OwnsInstance { get; }

    internal static SingleInstanceGuard Acquire(string? mutexName = null)
    {
        var mutex = new Mutex(
            initiallyOwned: true,
            mutexName ?? "Local\\OneBoardInlineTranslate.SingleInstance",
            out var createdNew);
        return createdNew
            ? new SingleInstanceGuard(mutex, true)
            : new SingleInstanceGuard(mutex, false);
    }

    public void Dispose()
    {
        if (OwnsInstance)
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The process is already shutting down and no longer owns the mutex.
            }
        }

        _mutex?.Dispose();
    }
}
