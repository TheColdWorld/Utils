using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using TheColdWorld.Utils.Thread;

namespace TheColdWorld.Utils.socket;

public sealed class TcpClient : IDisposable
{
    /// <param name="remoteEndPoint"><see cref="EndPoint"/> that <see cref="TcpClient"/> will connect to</param>
    /// <param name="onReceive">action will be executed whem <see cref="TcpClient"/> received a <see cref="IPacket"/></param>
    /// <param name="threadNamePrefix">The sting before the thread name(e.g <paramref name="threadNamePrefix"/>-index)</param>
    public TcpClient(EndPoint remoteEndPoint, Action<JsonObject, Identifier,SendToRemote> onReceive, string threadNamePrefix = "TheColdWorld-TcpClient-ThreadPool", CancellationToken cancellationToken = default)
    {
        Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        asyncService = new(threadNamePrefix, threadCount: 5);
        socket.Connect(remoteEndPoint);
        Connection = new(socket, asyncService, onReceive, connection => { }, _cts.Token,PacketBindSide.ServerBind);
    }
    public void Send(IPacket packet, SocketFlags flags = SocketFlags.None)
    {
        if (packet.PacketBindSide != PacketBindSide.ServerBind) throw new ArgumentException("Trying use client to send client bound packet", nameof(packet));
        Connection.Send(new(ref packet), flags);
    }
    internal readonly Connection Connection;
    internal readonly AsyncService asyncService;
    readonly CancellationTokenSource _cts;
    internal volatile bool _disposed = false;
    public void Dispose()
    {
        if (_disposed) return;
        lock (this)
        {
            if (_disposed) return;
            _disposed = true;
        }
        _cts.Cancel();
        Connection?.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
    ~TcpClient()
    {
        Dispose();
    }
}
