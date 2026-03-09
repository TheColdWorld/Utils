using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TheColdWorld.Utils;

public class Codec<T>(Codec<T>.Encoder<T> encoder, Codec<T>.Decoder<T> decoder) : JsonConverter<T>
{
    public delegate JsonNode Encoder<U>(U input);
    public delegate U Decoder<U>(JsonNode input);
    protected Encoder<T> encoder { get; } = encoder;
    protected Decoder<T> decoder { get; } = decoder;
    public virtual JsonNode Encode(T input) => encoder.Invoke(input);
    public virtual T Decode(JsonNode input) => decoder.Invoke(input);
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            if (reader.TokenType == JsonTokenType.Null) return default;
            JsonNode node = JsonSerializer.Deserialize<JsonNode>(ref reader, options) ?? throw new JsonException("Excepted a non-null json node");
            return decoder.Invoke(node);
        }
        catch (JsonException) { throw; }
        catch (Exception ex) { throw new JsonException(null, ex); }
    }
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        try
        {
            JsonNode node = encoder.Invoke(value);
            JsonSerializer.Serialize(writer, node, options);
        }
        catch (JsonException) { throw; }
        catch (Exception ex) { throw new JsonException(null, ex); }
    }
}
