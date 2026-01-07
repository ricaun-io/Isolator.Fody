[Isolator("Test")]
public class ClassIsolatorTest
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
}
