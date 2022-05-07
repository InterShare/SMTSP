namespace SMTSP.Entities.Content;

/// <summary>
/// Implement a custom smts content body
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SmtsContentAttribute : Attribute
{
    /// <summary>
    /// This name will be used to identify the content type and automatically parse it to this implementation.
    /// </summary>
    public string Name { get; set; }


    /// <param name="name"></param>
    public SmtsContentAttribute(string name)
    {
        Name = name;
    }
}