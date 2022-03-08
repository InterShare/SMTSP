namespace SMTSP.Extensions;

internal static class EnumHelper
{
    internal static string ToLowerCamelCaseString<TEnum>(this TEnum enumValue, string fallbackValue = null) where TEnum : struct, Enum
    {
        try
        {
            return enumValue.ToString().ToLowerCamelCase() ?? fallbackValue;
        }
        catch (Exception)
        {
            return fallbackValue;
        }
    }
}