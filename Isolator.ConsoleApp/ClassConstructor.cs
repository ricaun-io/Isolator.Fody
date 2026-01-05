[Isolator]
public class ClassConstructor
{
    private readonly string _name;
    public ClassConstructor() : this(null)
    {
    }
    public ClassConstructor(string name)
    {
        _name = name;
    }
    public bool Execute()
    {
        return !string.IsNullOrEmpty(_name);
    }
    public int Execute(int a)
    {
        return a;
    }
    public int Execute(int a, int b)
    {
        return a + b;
    }
}
