namespace SMTSP.Extensions;

internal static class AsyncExtension
{
    public static void RunAndForget(this Task task)
    {
        task.ConfigureAwait(false);
    }
}