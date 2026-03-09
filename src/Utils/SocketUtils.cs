using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TheColdWorld.Utils;
/// <summary>
/// Utils of <see cref="System.Net.Sockets.Socket"/>
/// </summary>
public static class SocketUtils
{
    internal const int BUFFER_LENGTH = 1024 * 1024;//1MB
    /// <summary>
    /// Receive a packet size of <paramref name="length"/> and convert it(a <see cref="Encoding.UTF8"/> <see langword="string"/>) to <see cref="JsonObject"/>
    /// </summary>
    /// <param name="stream">The <see cref="NetworkStream"/></param>
    /// <param name="length">size of packet</param>
    /// <returns>Async Task like <see cref="Receive(NetworkStream, long)"/></returns>
    /// <exception cref="InvalidOperationException">if <paramref name="stream"/> cannot read data</exception>
    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="length"/> <= <see langword="0"/></exception>
    /// <exception cref="InvalidDataException">is real receive size <= <paramref name="length"/></exception>
    /// <exception cref="JsonException">if Receive data is not a <see cref="Encoding.UTF8"/> <see cref="JsonObject"/></exception>
    /// <seealso cref="Receive(NetworkStream, long)"/>
    /// <seealso cref="ReceiveAsync(Socket, long, CancellationToken)"/>
    public static async Task<JsonObject> ReceiveAsync(this NetworkStream stream, long length, CancellationToken cancellation = default)
    {

        if (stream == null || !stream.CanRead) throw new InvalidOperationException();
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        long leftLen = length;
        FileInfo tempFile = new(Path.GetTempFileName());
        try
        {
            using (FileStream fileStream = tempFile.OpenWrite())
                while (leftLen > 0)
                    if (leftLen <= BUFFER_LENGTH)
                    {
                        byte[] buffer = new byte[leftLen];
                        if (await stream.ReadAsync(buffer, cancellation) != buffer.Length) throw new InvalidDataException();
                        await fileStream.WriteAsync(buffer, cancellation);
                        leftLen -= buffer.Length;
                    }
                    else
                    {
                        byte[] buffer = pool.Rent(BUFFER_LENGTH);
                        if (await stream.ReadAsync(buffer, 0, BUFFER_LENGTH, cancellation) != BUFFER_LENGTH) throw new InvalidDataException();
                        await fileStream.WriteAsync(buffer, 0, BUFFER_LENGTH, cancellation);
                        leftLen -= BUFFER_LENGTH;
                        pool.Return(buffer, true);
                    }
            using MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(tempFile.FullName, FileMode.Open);
            //using MemoryMappedViewAccessor accer = mmf.CreateViewAccessor(0, length, MemoryMappedFileAccess.Read);
            using MemoryMappedViewStream stream1 = mmf.CreateViewStream(0, length, MemoryMappedFileAccess.Read);
            return JsonSerializer.Deserialize<JsonObject>(stream1) is JsonObject obj ? obj : throw new JsonException();
        }
        finally
        {
            if (tempFile.Exists) tempFile.Delete();
        }
    }
    /// <summary>
    /// Receive a packet size of <paramref name="length"/> and convert it(a <see cref="Encoding.UTF8"/> <see langword="string"/>) to <see cref="JsonObject"/>
    /// </summary>
    /// <param name="socket">The <see cref="Socket"/></param>
    /// <param name="length">size of packet</param>
    /// <returns>Async Task like <see cref="Receive(Socket, long)"/></returns>
    /// <exception cref="InvalidOperationException">if <paramref name="socket"/> cannot receive data</exception>
    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="length"/> <= <see langword="0"/></exception>
    /// <exception cref="InvalidDataException">is real receive size <= <paramref name="length"/></exception>
    /// <exception cref="JsonException">if Receive data is not a <see cref="Encoding.UTF8"/> <see cref="JsonObject"/></exception>
    /// <seealso cref="Receive(Socket, long)"/>
    /// <seealso cref="ReceiveAsync(NetworkStream, long, CancellationToken)"/>
    public static async Task<JsonObject> ReceiveAsync(this Socket socket, long length, CancellationToken cancellation = default)
    {
        using NetworkStream stream = new(socket, false);
        return await stream.ReceiveAsync(length, cancellation);
    }
    /// <summary>
    /// Receive a packet size of <paramref name="length"/> and convert it(a <see cref="Encoding.UTF8"/> <see langword="string"/>) to <see cref="JsonObject"/>
    /// </summary>
    /// <param name="stream">The <see cref="NetworkStream"/></param>
    /// <param name="length">size of packet</param>
    /// <returns>A <see cref="JsonObject"/> <see langword="object"/> stands of data of packet</returns>
    /// <exception cref="InvalidOperationException">if <paramref name="stream"/> cannot read data</exception>
    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="length"/> <= <see langword="0"/></exception>
    /// <exception cref="InvalidDataException">is real receive size <= <paramref name="length"/></exception>
    /// <exception cref="JsonException">if Receive data is not a <see cref="Encoding.UTF8"/> <see cref="JsonObject"/></exception>
    /// <seealso cref="ReceiveAsync(NetworkStream, long, CancellationToken)"/>
    public static JsonObject Receive(this NetworkStream stream, long length)
    {
        if (stream == null || !stream.CanRead) throw new InvalidOperationException();
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        long leftLen = length;
        FileInfo tempFile = new(Path.GetTempFileName());
        try
        {
            using (FileStream fileStream = tempFile.OpenWrite())
                while (leftLen > 0)
                    if (leftLen <= BUFFER_LENGTH)
                    {
                        byte[] buffer = new byte[leftLen];
                        if (stream.Read(buffer, 0, buffer.Length) != buffer.Length) throw new InvalidDataException();
                        fileStream.Write(buffer);
                        leftLen -= buffer.Length;
                    }
                    else
                    {
                        byte[] buffer = pool.Rent(BUFFER_LENGTH);
                        if (stream.Read(buffer, 0, BUFFER_LENGTH) != BUFFER_LENGTH) throw new InvalidDataException();
                        fileStream.Write(buffer, 0, BUFFER_LENGTH);
                        leftLen -= BUFFER_LENGTH;
                        pool.Return(buffer, true);
                    }
            using MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(tempFile.FullName, FileMode.Open);
            //using MemoryMappedViewAccessor accer = mmf.CreateViewAccessor(0, length, MemoryMappedFileAccess.Read);
            using MemoryMappedViewStream stream1 = mmf.CreateViewStream(0, length, MemoryMappedFileAccess.Read);
            return JsonSerializer.Deserialize<JsonObject>(stream1) is JsonObject obj ? obj : throw new JsonException();
        }
        finally
        {
            if (tempFile.Exists) tempFile.Delete();
        }
    }
    /// <summary>
    /// Receive a packet size of <paramref name="length"/> and convert it(a <see cref="Encoding.UTF8"/> <see langword="string"/>) to <see cref="JsonObject"/>
    /// </summary>
    /// <param name="socket">The <see cref="Socket"/></param>
    /// <param name="length">size of packet</param>
    /// <returns>A <see cref="JsonObject"/> <see langword="object"/> stands of data of packet</returns>
    /// <exception cref="InvalidOperationException">if <paramref name="socket"/> cannot receive data</exception>
    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="length"/> <= <see langword="0"/></exception>
    /// <exception cref="InvalidDataException">is real receive size <= <paramref name="length"/></exception>
    /// <exception cref="JsonException">if Receive data is not a <see cref="Encoding.UTF8"/> <see cref="JsonObject"/></exception>
    /// <seealso cref="Receive(NetworkStream, long)"/>
    /// <seealso cref="ReceiveAsync(Socket, long, CancellationToken)"/>
    public static JsonObject Receive(this Socket socket, long length)
    {
        using NetworkStream stream = new(socket, false);
        return stream.Receive(length);
    }
    /// <summary>
    /// convert a <see cref="long"/> to a 8 size <see cref="byte[]"/>(big endian)
    /// </summary>
    public static byte[] ToByteArray(long value) => [
            (byte)((value >> 56) & 0xFF),
            (byte)((value >> 48) & 0xFF),
            (byte)((value >> 40) & 0xFF),
            (byte)((value >> 32) & 0xFF),
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF),
    ];
    public static long ToLong(byte[] value) => value.Length != 8
            ? throw new ArgumentException($"excepted a size-8 array,but got size {value.Length}", nameof(value))
            : ToLong(new ArraySegment<byte>(value));
    /// <summary>
    /// convert a <see cref="byte[]">(big endian)  to a <see cref="long">
    /// </summary>
    public static long ToLong(ArraySegment<byte> value)
    {
        ObjectUtil.ThrowIfNull(value, nameof(value));
        ObjectUtil.ThrowIfNull(value.Array, nameof(value.Array));
        if (value.Count != 8) throw new ArgumentException($"excepted a size-8 array,but got size {value.Count}", nameof(value));
        long data = 0;
        for (int i = 0; i < 8; i++)
        {
            data = (data << 8) | value.Array[value.Offset + i];
        }
        return data;
    }
    /// <summary>
    /// convert a <see cref="byte[]">(big endian)  to a <see cref="long">
    /// </summary>
    public static long ToLong(Memory<byte> value)
    {
        if (value.Length != 8) throw new ArgumentException($"excepted a size-8 array,but got size {value.Length}", nameof(value));
        long data = 0;
        for (int i = 0; i < 8; i++)
        {
            data = (data << 8) | value.Span[i];
        }
        return data;
    }
}
