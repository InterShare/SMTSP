namespace SMTSP.Extensions;

internal static class StreamExtension
{
    public static byte[] GetBytesWhile(this Stream stream, byte endByte)
    {
        try
        {
            var result = new List<byte>();

            while (true)
            {
                if (stream.CanSeek && stream.Position >= stream.Length)
                {
                    return result.ToArray();
                }

                var byteRead = (byte)stream.ReadByte();

                if (byteRead == endByte)
                {
                    break;
                }

                result.Add(byteRead);
            }

            return result.ToArray();
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    public static string GetStringTillEndByte(this Stream stream, byte endByte)
    {
        byte[] result = GetBytesWhile(stream, endByte);

        return result.Any() ? result.GetStringFromBytes() : "";
    }

    public static async Task CopyToAsyncWithProgress(this Stream source, Stream destination, IProgress<long>? progress, CancellationToken cancellationToken = default, int bufferSize = 81920)
    {
        var buffer = new byte[bufferSize];
        int bytesRead;
        long totalRead = 0;

        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            totalRead += bytesRead;
            progress?.Report(totalRead);
        }
    }
}