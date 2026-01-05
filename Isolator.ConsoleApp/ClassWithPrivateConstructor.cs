public class ClassWithPrivateConstructor : IsolatorInteface
{
    private bool Result { get; set; }
    private ClassWithPrivateConstructor() {
        Result = true;
    }
    public bool Execute()
    {
        return Result;
    }
}

public class ClassWithPublicConstructor : IsolatorInteface
{
    private bool Result { get; set; }
    public ClassWithPublicConstructor()
    {
        Result = true;
    }
    public bool Execute()
    {
        return Result;
    }
}

public class ClassWithAbstractionConstructor : IsolatorAbstract
{
    public override bool Execute()
    {
        return Result;
    }
}

public abstract class IsolatorAbstract : IsolatorInteface
{
    public IsolatorAbstract()
    {
        Result = true;
    }
    protected bool Result { get; set; }
    public abstract bool Execute();
}

[Isolator]
public static class ClassWithStaticConstructor
{
    private static bool Result { get; set; }
    static ClassWithStaticConstructor()
    {
        Result = true;
    }
    public static bool Execute()
    {
        return Result;
    }
}

public interface IsolatorInteface
{
    bool Execute();
}