using System.Collections.Concurrent;
using System.Diagnostics;
using TheColdWorld.Utils.Exceptions;
using TheColdWorld.Utils.ObjectPools;

namespace TheColdWorld.Utils.Thread;
#pragma warning disable CS0618
/// <summary>
/// A thread pool out of <see cref="System.Threading.ThreadPool"/><br/>Please use <see cref="AsyncService"/> instead of <see cref="ThreadPool"/>
/// </summary>
/// <seealso cref="AsyncService"/>
[Obsolete("use TheColdWorld.Utils.Thread.AsyncService instead of TheColdWorld.Utils.Thread.ThreadPool", false)]
public sealed class ThreadPool : TaskScheduler, IDisposable
{
    public override int MaximumConcurrencyLevel => (int)unitpool.Capacity;
    private readonly string namePrefix;
    private readonly ThreadPriority threadPriority;
    private bool _disposed;
    private readonly ObjectArrayPool<ThreadUnit> unitpool;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly ConcurrentDictionary<ThreadUnit, PoolObject<ThreadUnit>> gotten = [];
    private readonly ConcurrentQueue<Task> scheculedTasks = [];
    public ThreadPool(string prefix, ThreadPriority priority = ThreadPriority.Normal, uint? threadCount = null)
    {
        uint _count = threadCount == null ? (uint)Environment.ProcessorCount : threadCount.Value;
        if (_count > Environment.ProcessorCount * 1000) throw new IndexOutOfRangeException($"param {nameof(threadCount)} is tooooooooo big ({_count}>{Environment.ProcessorCount * 1000})");
        this.namePrefix = prefix;
        this.threadPriority = priority;
        unitpool = new(_count,constructorWithIndex:index => new ThreadUnit(this,index,cancellationTokenSource.Token), (ref unit) => { });
    }
    protected override IEnumerable<Task> GetScheduledTasks() => _disposed? throw new ObjectDisposedException(nameof(ThreadPool)): new List<Task>(scheculedTasks);
    protected override void QueueTask(Task task)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ThreadPool));
        if (unitpool.TryRent(out PoolObject<ThreadUnit> unit))
        {
            if (!gotten.TryAdd(unit.Value, unit)) throw new System.InvalidOperationException("failed to update ThreadPool status");
            unit.Value.Awake(task);
        }
        else scheculedTasks.Enqueue(task);
    }
    protected override Boolean TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    internal sealed class ThreadUnit :IDisposable
    {
        internal ThreadUnit(in ThreadPool threadPool, uint index,CancellationToken token=default)
        {
            this.father=threadPool;
            this.source=CancellationTokenSource.CreateLinkedTokenSource(token);
            thread = new(Loop) { Name=$"{father.namePrefix}-{index}",Priority=father.threadPriority,IsBackground=true};
            thread.Start();
        }
        internal readonly System.Threading.Thread thread;
        internal readonly ThreadPool father;
        private Task? _cutrrentTask=null;
        private readonly CancellationTokenSource source;
        private readonly object _lock = new();
        private bool running = false;
        private void Loop()
        {
            while(!source.Token.IsCancellationRequested)
            {
                lock (_lock)
                {
                    while(!running)
                    {
                        Monitor.Wait(_lock);
                    }
                }
                if (_cutrrentTask is not null)
                {
                    if (!father.TryExecuteTask(_cutrrentTask))
                    {
                        if (_cutrrentTask.IsFaulted)
                        {
                            Logging.Log(Logging.LogLevel.Error, "occored a unhandled Exception", _cutrrentTask.Exception);
                        }
                        if (_cutrrentTask.IsCanceled){
                            Logging.Log(Logging.LogLevel.Infomation, "Task in threadpool is canceled");
                        }
                    }
                }
                _cutrrentTask = null;
                if (father.scheculedTasks.TryDequeue(out Task newTask))
                {
                    this._cutrrentTask = newTask;
                    continue;
                }
                //on idle
                lock (_lock) {
                    running = false;
                    if (father.gotten.TryRemove(this, out PoolObject<ThreadUnit> reference))
                    {
                        father.unitpool.Return(reference);
                    }
                }
            }
        }
        internal void Awake(Task newTask)
        {
            lock (_lock)
            {
                _cutrrentTask = newTask;
                running = true;
                Monitor.PulseAll(_lock); 
            }
        }

        public void Dispose()
        {
            this.source.Cancel();
            this.source.Dispose();
        }
    }
    private void Dispose(Boolean disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                this.cancellationTokenSource.Cancel();
                this.unitpool.Dispose();
            }
            _disposed = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
#pragma warning restore