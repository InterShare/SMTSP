namespace SMTSP.Extensions;

internal static class StreamExtension
{
    internal static async Task CopyToAsyncWithProgress(this Stream source, Stream destination, IProgress<long>? progress, CancellationToken cancellationToken = default, int bufferSize = 81920)
    {
        var buffer = new byte[bufferSize];
        int bytesRead;
        long totalRead = 0;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            totalRead += bytesRead;
            progress?.Report(totalRead);
        }
    }
}
