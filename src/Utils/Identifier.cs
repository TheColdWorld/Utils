
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheColdWorld.Utils;

[DebuggerDisplay("{ToString()}")]
[JsonConverter(typeof(Serializer))]
public sealed class Identifier(string @namespace, string path) : IEquatable<Identifier>
{
    public Identifier(string id) : this(string.Empty, string.Empty)
    {
        string[] data = id.Split(':', 2);
        if (data.Any(s => string.IsNullOrWhiteSpace(s))) throw new ArgumentException($"Excepted string(not null or white space):string(not null or white space),but currrent{id}", nameof(id));
        this.Namespace = data[0];
        this.Path = data[1];
    }
    string Namespace { get; } = @namespace;
    string Path { get; } = path;
    public byte[] AsByte() => Encoding.UTF8.GetBytes(ToString());
    public byte[] AsByte(Encoding encoding) => encoding.GetBytes(ToString());
    public bool Equals(Identifier other) => other.Namespace == Namespace && other.Path == Path;
    public override bool Equals(object? obj) => obj is Identifier id ? Equals(id) : obj is string s && s == ToString();
    public override string ToString() => Namespace + ":" + Path;
    public override int GetHashCode() => HashCode.Combine(this.Namespace, this.Path);
    public static bool operator ==(Identifier a, Identifier? b) => b is not null && a.Equals(b);
    public static bool operator !=(Identifier a, Identifier? b) => !(a == b);
    public static implicit operator string(Identifier identifier) => identifier.ToString();
    public class Serializer : JsonConverter<Identifier>
    {
        public override Identifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.String) throw new JsonException($"TypeToken is wrong excepted:Sting ,current:{reader.TokenType}");
            //if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException($"TypeToken is wrong excepted:Sting ,current:{reader.TokenType}");
            string data = reader.GetString()!;
            return new Identifier(data);
        }
        public override void Write(Utf8JsonWriter writer, Identifier value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }
}
