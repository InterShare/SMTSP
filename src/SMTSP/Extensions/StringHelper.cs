namespace SMTSP.Extensions;

internal static class StringHelpers
{
    internal static string? ToLowerCamelCase(this string? value)
    {
        return value?.Length > 1 ? char.ToLowerInvariant(value[0]) + value.Substring(1) : value;
    }

    internal static TEnum ToEnum<TEnum>(this string value, TEnum? fallbackValue = null) where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            if (fallbackValue != null)
            {
                return (TEnum) fallbackValue;
            }

            return default(TEnum);
        }

        return Enum.TryParse<TEnum>(value, true, out TEnum result) ? result : default(TEnum);
    }
}