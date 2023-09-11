namespace SMTSP.Extensions;

public static class StringExtensions
{
    internal static string? ToLowerCamelCase(this string? value)
    {
        return value?.Length > 1 ? char.ToLowerInvariant(value[0]) + value[1..] : value;
    }

    internal static TEnum ToEnum<TEnum>(this string value, TEnum? fallbackValue = null) where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            if (fallbackValue != null)
            {
                return (TEnum) fallbackValue;
            }

            return default;
        }

        return Enum.TryParse(value, true, out TEnum result) ? result : default;
    }
}
