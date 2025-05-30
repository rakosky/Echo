public class ReadWriteLock
{
    private bool _readLocked;
    private event Action? ReadLockReleasedEvent;

    /// <summary>
    ///  Call when you want to block readers.
    /// </summary>
    public void AcquireReadLock()
    {
        _readLocked = true;
    }

    /// <summary>
    ///  Call when you’re done—and fire any waiters.
    /// </summary>
    public void ReleaseReadLock()
    {
        _readLocked = false;
        ReadLockReleasedEvent?.Invoke();
        // we don’t null out the event—handlers remove themselves
    }

    /// <summary>
    ///  Await until the read‐lock is released.  Returns immediately if it’s not held.
    /// </summary>
    public Task WaitForReadLockReleaseAsync(CancellationToken ct = default)
    {
        // if nobody’s holding the lock, return a completed task
        if (!_readLocked)
            return Task.CompletedTask;

        // otherwise set up a one‐time waiter
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action? handler = null;
        handler = () =>
        {
            // unsubscribe first to avoid leaks/races
            ReadLockReleasedEvent -= handler;
            tcs.TrySetResult(null);
        };

        // attach
        ReadLockReleasedEvent += handler;

        // cancellation support
        if (ct.CanBeCanceled)
        {
            ct.Register(() =>
            {
                ReadLockReleasedEvent -= handler;
                tcs.TrySetCanceled(ct);
            }, useSynchronizationContext: false);
        }

        return tcs.Task;
    }
}
