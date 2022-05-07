namespace SMTSP.Entities.Content;

/// <summary>
/// Include this property in the body of the data transmission.
/// IMPORTANT: Do not include a 0x00 byte. Since this will corrupt the body data.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IncludeInBodyAttribute : Attribute
{
}