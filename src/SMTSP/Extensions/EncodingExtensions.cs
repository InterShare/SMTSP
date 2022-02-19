using System.Text;

namespace SMTSP.Extensions;

internal static class EncodingExtensions
{
    public static IEnumerable<byte>? GetBytes(this string? value)
    {
        return value == null ? null : Encoding.UTF8.GetBytes(value);
    }

    public static string GetStringFromBytes(this byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}