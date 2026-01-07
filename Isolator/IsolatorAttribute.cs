using System;
/// <summary>
/// Initialize a new instance of <see cref="IsolatorAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IsolatorAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="IsolatorAttribute"/>
    /// </summary>
    public IsolatorAttribute(string contextName = null)
    {
        ContextName = contextName;
    }

    /// <summary>
    /// Gets the name of the current context.
    /// </summary>
    public string ContextName { get; }
}