using System.ComponentModel;
using System.Reflection;
using SMTSP.Extensions;

namespace SMTSP.Entities.Content;

/// <summary>
/// Abstract representation of the content that will be transmitted.
/// </summary>
public abstract class SmtspContent
{
    /// <summary>
    /// The data stream.
    /// </summary>
    public Stream DataStream { get; set; }

    public IEnumerable<byte> ToBinary()
    {
        var body = new List<byte>();

        var properties = GetType().GetProperties(
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public);

        foreach (PropertyInfo property in properties)
        {
            bool shouldBeIncludedInBody = GetType()
                .GetProperty(property.Name)!
                .GetCustomAttributes(true)
                .Any(a => a.GetType().Name == nameof(IncludeInBodyAttribute));

            if (shouldBeIncludedInBody)
            {
                string? value = property.PropertyType.IsEnum
                    ? property.GetValue(this)?.ToString().ToLowerCamelCase()
                    : property.GetValue(this)?.ToString();

                if (!string.IsNullOrEmpty(value))
                {
                    body.AddRange($"{property.Name}={value}".GetBytes());
                    body.Add(0x00);
                }
            }
        }

        return body;
    }

    public void FromStream(Stream stream)
    {
        try
        {
            var properties = GetType().GetProperties(
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);

            foreach (PropertyInfo _ in properties)
            {
                string currentPropertyAndValue = stream.GetStringTillEndByte(0x00);

                if (!currentPropertyAndValue.Contains("=")) { continue; }

                int index = currentPropertyAndValue.IndexOf('=');
                string propertyName = currentPropertyAndValue.Substring(0, index);
                string propertyValue = currentPropertyAndValue.Substring(index + 1);

                PropertyInfo? property = properties.FirstOrDefault(p => p.Name == propertyName);

                if (property == null) { continue; }

                object? convertedPropertyValue;

                Type propType = property.PropertyType;

                if (propType == typeof(string))
                {
                    convertedPropertyValue = propertyValue;
                }
                else
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(propType);
                    convertedPropertyValue = converter.ConvertFromString(propertyValue);
                }

                property.SetValue(this, convertedPropertyValue);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}