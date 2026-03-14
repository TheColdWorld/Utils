using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using TheColdWorld.Utils.Thread;

namespace TheColdWorld.Utils.socket;

internal sealed class Connection : IDisposable
{
    internal Connection(Socket client, AsyncService asyncService, Action<JsonObject, Identifier,SendToRemote> packetAccept, Action<Connection> onDispose, CancellationToken token,PacketBindSide remoteSide)
    {
        this.socket = client;
        this._cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        this.asyncService = asyncService;
        this.packetAccept = packetAccept;
        this.onDispose = onDispose;
        this.remoteSide=remoteSide;
        SendQueue = new();
        RecvTask = asyncService.Run(RecvLoop, _cts.Token);
        SendTask = asyncService.Run(SendLoop, _cts.Token);
    }
    private async Task RecvLoop()
    {
        while (stable && !_cts.IsCancellationRequested)
        {
            try
            {
                int totalReceived = 0;
                while (totalReceived < buffer.Length)
                {
                    int received = await socket.ReceiveAsync(buffer[totalReceived..], SocketFlags.None, _cts.Token).ConfigureAwait(false);
                    if (received == 0)
                    {
                        _ = asyncService.Run(Dispose);
                        return;
                    }
                    totalReceived += received;
                }
                long len = SocketUtils.ToLong(buffer);
                JsonObject result = await SocketUtils.ReceiveAsync(socket, len, _cts.Token);
                if (result.ContainsKey("data") && result.ContainsKey("id") && result["data"] is JsonObject data && result["id"] is JsonValue jv)
                {
                    Identifier id = new(jv.ToString());
                    _ = asyncService.Run(() =>
                    {
                        try
                        {
                            this.packetAccept.Invoke(data, id, (packet, flag, token) =>
                            {
                                if (packet.PacketBindSide != remoteSide) throw new ArgumentException("Trying use server to send server bound packet", nameof(packet));
                                Packet<IPacket> _willsend = new(ref packet);
                                Send(_willsend, flag, token);
                            });
                        }
                        catch (Exception e)
                        {
                            Logging.Log(Logging.LogLevel.Error, $"Exception occored in Packet accept", e);
                        }
                    });
                }
            }
            catch (SocketException se)
            {
                Logging.Log(Logging.LogLevel.Error, "SocketException occored in recving data", se);
                _ = asyncService.Run(Dispose);
                return;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception e)
            {
                Logging.Log(Logging.LogLevel.Error, $"Exception occored in Packet decode", e);
            }
        }
    }
    private async Task SendLoop()
    {
        while (!_disposed && stable && !_cts.IsCancellationRequested)
        {
            if (SendQueue.TryDequeue(out Func<Task> task))
            {
                try
                {
                    await task.Invoke();
                }
                catch (SocketException se)
                {
                    Logging.Log(Logging.LogLevel.Error, "SocketException occored in sending data", se);
                    _ = asyncService.Run(Dispose);
                    return;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Logging.Log(Logging.LogLevel.Error, $"Exception occored in Packet send", e);
                }
            }
        }
    }
    internal void Send<TPacket>(Packet<TPacket> packet, SocketFlags flags = SocketFlags.None, CancellationToken cancellationToken = default) where TPacket : class, IPacket
    {
        SendQueue.Enqueue(async () =>
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
            await socket.SendAsync(packet.willSendData, flags, cts.Token);
        });
    }
    internal void Send(Packet<IPacket> packet, SocketFlags flags = SocketFlags.None, CancellationToken cancellationToken = default)
    {
        SendQueue.Enqueue(async () =>
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
            await socket.SendAsync(packet.willSendData, flags, cts.Token);
        });
    }
    public void Dispose()
    {
        if (_disposed) return;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            stable = false;
        }
        _cts.Cancel();
        TimeSpan timeout = new(0, 0, 5);
        try
        {
            RecvTask.Wait(timeout);
            SendTask.Wait(timeout);
        }
        catch (AggregateException) { }
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch { }
        socket.Close();
        socket.Dispose();
        _cts.Dispose();
        SendQueue.Clear();
        onDispose.Invoke(this);
        GC.SuppressFinalize(this);
    }
    internal PacketBindSide remoteSide;
    internal void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(socket)); }
    internal AsyncService asyncService;
    internal ConcurrentQueue<Func<Task>> SendQueue;
    internal Socket socket;
    internal Task RecvTask;
    internal Task SendTask;
    internal readonly CancellationTokenSource _cts;
    private volatile bool _disposed;
    private volatile bool stable = true;
    private readonly object _lock = new();
    private readonly Action<JsonObject, Identifier,SendToRemote> packetAccept;
    private readonly Action<Connection> onDispose;
    private readonly Memory<byte> buffer = new byte[8];
    ~Connection()
    {
        Dispose();
    }
}
