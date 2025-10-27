using System;
using System.Collections.Generic;
using System.Text;

namespace TheColdWorld.Utils.Thread;
/// <summary>
/// A class that can run <see cref="System.Threading.Tasks.Task"/> on a ThreadPool out of <see cref="System.Threading.ThreadPool"/>
/// </summary>
///<seealso cref="ThreadPool"/>
public class AsyncService : IDisposable
{
    /// <param name="prefix">The sting before the thread name(e.g <paramref name="prefix"/>-index)</param>
    /// <param name="priority">The thread priority in thread pool</param>
    /// <param name="threadCount">The thread count of the thread pool(Default:<see cref="Environment.ProcessorCount"/>)</param>
    public AsyncService(string prefix, ThreadPriority priority = ThreadPriority.Normal, uint? threadCount = null) => threadPool = new(prefix, priority, threadCount);
    protected ThreadPool threadPool ;
    private bool disposed = false;

    public Task<TResult> Run<TResult>(Func<Task<TResult>> func)  => disposed
            ? throw new ObjectDisposedException(nameof(AsyncService))
            : Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, threadPool).Unwrap();
    public Task<TResult> Run<TResult>(Func<TResult> func)  => disposed
            ? throw new ObjectDisposedException(nameof(AsyncService))
            : Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, threadPool); 
    public Task Run(Func<Task> func) => disposed
            ? throw new ObjectDisposedException(nameof(AsyncService))
            : Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, threadPool).Unwrap();
    public Task Run(Action func) => disposed
            ? throw new ObjectDisposedException(nameof(AsyncService))
            : Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, threadPool);

    protected virtual void Dispose(Boolean disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                threadPool.Dispose();
            }
            disposed = true;
        }
    }
    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
