[Isolator("ClassIsolator")]
public class ClassIsolator
{
    public bool Execute()
    {
        return !this.IsContextDefault();
    }
    public bool ExecuteClass()
    {
        var instance = new Class();
        return !instance.IsContextDefault();
    }
    public bool ExecuteClass(bool value)
    {
        var instance = new Class();
        return instance.Execute(value);
    }
    public bool ExecuteContextNumber()
    {
        var instance = new Class();
        return instance.ContextNumber() != ContextNumber();
    }
    public int ContextNumber()
    {
        return this.ToContextNumber();
    }
}
