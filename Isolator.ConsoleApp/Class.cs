[Isolator]
public class Class
{
    public bool Execute()
    {
        return !this.IsContextDefault();
    }
    public bool Execute(bool value)
    {
        return value;
    }
    public bool Execute(string value)
    {
        return !string.IsNullOrEmpty(value);
    }
    public bool Execute(int value)
    {
        return value != 0;
    }
    public bool ExecuteOut(string value, out string result)
    {
        result = value;
        return !string.IsNullOrEmpty(value);
    }
    public bool ExecuteRef(string value, ref string result)
    {
        result = value;
        return !string.IsNullOrEmpty(value);
    }
    public string ContextName()
    {
        return this.ToContextString();
    }
    public string ContextNameFail()
    {
        // This return the default context name because of the typeof usage in the class.
        return typeof(Class).ToContextString();
    }
    public override string ToString()
    {
        return this.ToContextString();
    }
}
