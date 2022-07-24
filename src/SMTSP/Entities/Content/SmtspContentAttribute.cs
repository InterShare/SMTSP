namespace SMTSP.Entities.Content;

/// <summary>
/// Implement a custom smts content body
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SmtspContentAttribute : Attribute
{
    /// <summary>
    /// This name will be used to identify the content type and automatically parse it to this implementation.
    /// </summary>
    public string Name { get; }


    /// <param name="name"></param>
    public SmtspContentAttribute(string name)
    {
        Name = name;
    }
}