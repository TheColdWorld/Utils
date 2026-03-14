using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace TheColdWorld.Utils.socket;

public delegate void SendToRemote(IPacket i, SocketFlags flags = SocketFlags.None, CancellationToken token = default);
public sealed class TcpServer : IDisposable
{
    /// <param name="port">the <see cref="TcpServer"/>'s open port</param>
    /// <param name="PacketAccept">action will be executed whem <see cref="TcpServer"/> received a <see cref="IPacket"/></param>
    /// <param name="backlog">The maximum length of the pending connections queue.</param>
    /// <param name="enableV4">enable <see cref="TcpServer"/> on Ipv4 port</param>
    /// <param name="enableV6">enable <see cref="TcpServer"/> on Ipv6 port</param>
    /// <param name="threadNamePrefix">The sting before the thread name(e.g <paramref name="threadNamePrefix"/>-index)</param>
    /// <exception cref="ArgumentException">throws if <paramref name="enableV4"/>=<paramref name="enableV6"/>=false</exception>
    public TcpServer(int port, Action<JsonObject, Identifier,SendToRemote> PacketAccept, uint backlog = 20, bool enableV4 = true, bool enableV6 = true, string threadNamePrefix = "TheColdWorld-TcpServer-ThreadPool", CancellationToken cancellationToken = default)
    {
        if (!enableV4 && !enableV6) throw new ArgumentException($"You cannot set {nameof(enableV4)}={nameof(enableV6)}=false");
        this.asyncService = new(threadNamePrefix, ThreadPriority.Normal, backlog * 3);
        packetAccept = PacketAccept;
        this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Socket.OSSupportsIPv4)
        {
            if (enableV4)
            {
                v4_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                v4_socket.Bind(new IPEndPoint(IPAddress.Any, port));
                v4_socket.Listen((int)backlog);
                asyncService.Run(async () => await BeginAccept(v4_socket), cancellationTokenSource.Token);
            }
        }
        else Logging.Log(Logging.LogLevel.Warning, $"TcpServer@{ObjectUtil.GetHashCodeHexString(this)}:System.Net.Sockets.Socket.OSSupportsIPv4 is false,ipv4 Tcp server will be disabled");
        if (Socket.OSSupportsIPv6)
        {
            if (enableV6)
            {
                v6_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                v6_socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                v6_socket.Listen((int)backlog);
                asyncService.Run(async () => await BeginAccept(v6_socket), cancellationTokenSource.Token);
            }
        }
        else Logging.Log(Logging.LogLevel.Warning, $"TcpServer@{ObjectUtil.GetHashCodeHexString(this)}:System.Net.Sockets.Socket.OSSupportsIPv6 is false,ipv6 Tcp server will be disabled");
    }
    private async Task BeginAccept(Socket socket)
    {
        try
        {
            Socket client = await socket.AcceptAsync();
            lock (clients)
            {
                Connection connection = new(client, asyncService, packetAccept, c => { lock (clients) { clients.Remove(c); } }, cancellationTokenSource.Token,PacketBindSide.ClientBind);
                clients.AddLast(connection);
            }
            await BeginAccept(socket);
        }
        catch (ObjectDisposedException) when (this.disposedValue) { }
        catch (Exception ex)
        {
            Logging.Log(Logging.LogLevel.Error, "Exception occored in accepting connections,retrying in 10 seconds", ex);
            await Task.Delay(new TimeSpan(0, 0, 10));
            await BeginAccept(socket);
        }
    }
    public async Task BroadCastAsync<TPacket>(TPacket packet, SocketFlags flags = SocketFlags.None, CancellationToken token = default) where TPacket : class, IPacket
    {
        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationTokenSource.Token);
        if (packet.PacketBindSide != PacketBindSide.ClientBind) throw new ArgumentException("Trying use server to send server bound packet", nameof(packet));
        Packet<TPacket> willsend = new(ref packet);
        lock (clients)
        {
            foreach (var client in clients)
            {
                client.Send(willsend, flags, source.Token);
            }
        }
    }
    public async Task BroadCastAsync(IPacket packet, SocketFlags flags = SocketFlags.None, CancellationToken token = default)
    {
        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationTokenSource.Token);
        if (packet.PacketBindSide != PacketBindSide.ClientBind) throw new ArgumentException("Trying use server to send server bound packet", nameof(packet));
        Packet<IPacket> willsend = new(ref packet);
        lock (clients)
        {
            foreach (var client in clients)
            {
                client.Send(willsend, flags, source.Token);
            }
        }
    }
    readonly CancellationTokenSource cancellationTokenSource;
    private readonly Socket? v4_socket;
    private readonly Socket? v6_socket;
    private readonly Action<JsonObject, Identifier,SendToRemote> packetAccept;
    readonly LinkedList<Connection> clients = [];
    readonly Thread.AsyncService asyncService;
    private Boolean disposedValue;
    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                Connection[] connections;
                lock (clients) { connections = [.. clients]; }
                foreach (var item in connections)
                {
                    item?.Dispose();
                }
                lock (clients)
                {
                    clients.Clear();
                }
                v4_socket?.Dispose();
                v6_socket?.Dispose();
            }
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
