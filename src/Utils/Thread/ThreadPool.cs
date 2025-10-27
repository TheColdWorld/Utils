using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace TheColdWorld.Utils.Thread;

/// <summary>
/// A thread pool out of <see cref="System.Threading.ThreadPool"/><br/>Please use <see cref="AsyncService"/> instead of <see cref="ThreadPool"/>
/// </summary>
/// <seealso cref="AsyncService"/>
public sealed class ThreadPool : TaskScheduler, IDisposable
{
    public ThreadPool(string prefix,ThreadPriority priority=ThreadPriority.Normal,uint? threadCount=null)
    {
        tasks = [];
        this.priority = priority;
        this.threadNamePrefix=prefix;
        cancellationTokenSource = new();
        if(threadCount != null)
        {
            if(threadCount == 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
            threads = new ThreadUnit[threadCount.Value];
        }
        else
        {
            threads = new ThreadUnit[Environment.ProcessorCount];
        }
        for (uint i = 0; i < threads.Length; i++)
        {
            threads[i] = new(this, i);
        }
    }
    internal readonly BlockingCollection<Task> tasks;
    internal readonly ThreadUnit[] threads;
    internal readonly string threadNamePrefix;
    internal readonly ThreadPriority priority;
    internal readonly CancellationTokenSource cancellationTokenSource;
    private volatile bool disposed;
    protected override IEnumerable<Task> GetScheduledTasks() => tasks;
    protected override void QueueTask(Task task)
    {
        if(disposed) throw new ObjectDisposedException(GetType().Name);
        tasks.Add(task);
    }
    protected override Boolean TryExecuteTaskInline(Task task, Boolean taskWasPreviouslyQueued) => !disposed && Array.Exists(threads, t => t.thread == System.Threading.Thread.CurrentThread) && TryExecuteTask(task);
    internal sealed class ThreadUnit
    {
        internal ThreadUnit(in ThreadPool threadPool,uint index) {
            this.father = threadPool;
            thread = new(Tick)
            {
                Name =$"{threadPool.threadNamePrefix}-{index}",
                IsBackground=true,
                Priority= threadPool.priority
            };
            Logging.Log(Logging.LogLevel.Debug, $"Thread {thread.Name} started");
            thread.Start(); 
        }
        internal readonly System.Threading.Thread thread;
        internal readonly ThreadPool father;
        internal void Tick()
        {
            while (!father.cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    Task CurrentTask = father.tasks.Take(father.cancellationTokenSource.Token);
                    try
                    {
                        father.TryExecuteTask(CurrentTask);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogLevel.Error,$"Exception occored in thread {thread.Name}", ex);
                    }
                }
                catch(OperationCanceledException) { break; }
                catch (Exception e) when (IsCriticalThreadException(e)) {
                    Logging.Log(Logging.LogLevel.Error, $"Thread {thread.Name} exited because", e);
                    break; 
                }
                catch (Exception) { continue; }
            }
        }
    }
    public enum ThreadStatus
    {
        Idle,UnInitlaized,Working
    }

    private void Dispose(Boolean disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                tasks.CompleteAdding();
                cancellationTokenSource.Cancel();
                foreach (var item in this.threads)
                {
                    if (item.thread.IsAlive) item.thread.Join();
                }
                cancellationTokenSource.Dispose();
                this.tasks.Dispose();
            }
            disposed = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    internal  static bool IsCriticalThreadException(Exception ex) => ex is ObjectDisposedException or
               ThreadAbortException or
               AppDomainUnloadedException or
               OutOfMemoryException or
               AccessViolationException ;
    
}
