using System.Text.Json.Nodes;

namespace TheColdWorld.Utils;

public class Codec<T>(Codec<T>.Encoder<T> encoder, Codec<T>.Decoder<T> decoder)
{
    public delegate JsonNode Encoder<U>(U input);
    public delegate U Decoder<U>(JsonNode input);
    protected Encoder<T> encoder { get; } = encoder;
    protected Decoder<T> decoder { get; } = decoder;
    public JsonNode Encode(T input) => encoder.Invoke(input);
    public T Decode(JsonNode input) => decoder.Invoke(input);
}
