using System.Net;
using System.Net.Http.Headers;
using System.Security;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using TheColdWorld.Utils;
using TheColdWorld.Utils.socket;

namespace ConsoleDebug
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logging.SetLogger((level, message) => Console.WriteLine($"[{level}]{message}"));
            int port = 25564;
            CancellationTokenSource tokenSource = new ();
            using TcpServer server = new(port, (json, id) => 
            Console.WriteLine($"[Debug]Server accepted packet(ID:{id}):{json.ToJsonString(new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) })}"));
            using TcpClient client = new(new IPEndPoint(IPAddress.Loopback,port), (json, id) => 
            Console.WriteLine($"[Debug]Client accepted packet(ID:{id}):{json.ToJsonString(new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) })}"));
            using TcpClient client2 = new(new IPEndPoint(IPAddress.Loopback, port), (json, id) => 
            Console.WriteLine($"[Debug]Client accepted packet(ID:{id}):{json.ToJsonString(new() { Encoder=JavaScriptEncoder.Create(UnicodeRanges.All)})}"));
            server.BroadCastAsync(new TestPacket2()).GetAwaiter().GetResult();
            client.Send(new TestPacket());
            client2.Send(new TestPacket());
            client.Send(new TestPacket());
            try
            {
                client.Send(new TestPacket2());
            }
            catch (ArgumentException) { Console.WriteLine("[Debug] successfully got a true exception client->client"); }
            try
            {
                server.BroadCastAsync(new TestPacket()).GetAwaiter().GetResult();
            }
            catch (ArgumentException) { Console.WriteLine("[Debug] successfully got a true exception server->server"); }
            Console.ReadKey(true);
            tokenSource.Cancel ();
        }
    }
    class TestPacket : IPacket
    {
        public PacketBindSide PacketBindSide => PacketBindSide.ServerBind;

        public Identifier Identifier => new("test","serverbind");

        public JsonObject Write() => new () { ["int"] = 1, ["str"]="客户端：我的包啊" };
    }
    class TestPacket2 : IPacket
    {
        public PacketBindSide PacketBindSide => PacketBindSide.ClientBind;

        public Identifier Identifier => new("test", "clientbound");

        public JsonObject Write() => new() { ["int"] = 2, ["str"] = "服务端：我的包啊" };
    }
}
