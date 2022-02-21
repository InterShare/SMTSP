using System.Text;

namespace SMTSP.Extensions;

internal static class EncodingExtensions
{
    internal static IEnumerable<byte> GetBytes(this string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    internal static string GetStringFromBytes(this byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}