namespace Isolator.ConsoleApp;

[Isolator]
public static class ClassStatic
{
    public static bool Execute()
    {
        return !System.Reflection.Assembly.GetExecutingAssembly().IsContextDefault();
    }
    public static bool Execute(bool value)
    {
        return value;
    }
    public static bool Execute(string value)
    {
        return !string.IsNullOrEmpty(value);
    }
    public static bool Execute(int value)
    {
        return value != 0;
    }
    public static bool ExecuteOut(string value, out string result)
    {
        result = value;
        return !string.IsNullOrEmpty(value);
    }
    public static bool ExecuteRef(string value, ref string result)
    {
        result = value;
        return !string.IsNullOrEmpty(value);
    }
    public static string ContextName()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().ToContextString();
    }
    public static string ContextNameFail()
    {
        // This return the default context name because of the typeof usage in a static class.
        return typeof(ClassStatic).ToContextString();
    }
}
