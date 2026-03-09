using System.Text;
using System.Text.Json.Nodes;

namespace TheColdWorld.Utils.socket;

public interface IPacket
{
    PacketBindSide PacketBindSide { get; }
    Identifier Identifier { get; }
    public JsonObject Write();
}
internal sealed class Packet<P> where P : class, IPacket
{
    internal Packet(ref P packet)
    {
        ObjectUtil.ThrowIfNull(packet);
        this.packet = packet;
        JsonObject packetObj = new()
        {
            ["id"] = Identifier.ToString(),
            ["data"] = packet.Write()
        };
        byte[] data = Encoding.UTF8.GetBytes(packetObj.ToJsonString());
        willSendData = new byte[data.Length + 8];
        SocketUtils.ToByteArray(data.LongLength).CopyTo(willSendData, 0);
        data.CopyTo(willSendData, 8);
    }
    internal byte[] willSendData;
    internal P packet;
    internal Identifier Identifier => packet.Identifier;
}