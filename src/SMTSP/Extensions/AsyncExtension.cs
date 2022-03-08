namespace SMTSP.Extensions;

internal static class AsyncExtension
{
    internal static void RunAndForget(this Task task)
    {
        task.ConfigureAwait(false);
    }
}