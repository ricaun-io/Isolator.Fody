namespace Isolator.ConsoleApp;

public class ClassWithPrivateConstructor : IsolatorInterface
{
    private bool Result { get; set; }
    private ClassWithPrivateConstructor() {
        Result = true;
    }
    public bool Execute()
    {
        return Result && !this.IsContextDefault();
    }
}

public class ClassWithPublicConstructor : IsolatorInterface
{
    private bool Result { get; set; }
    public ClassWithPublicConstructor()
    {
        Result = true;
    }
    public bool Execute()
    {
        return Result && !this.IsContextDefault();
    }
}

public class ClassWithAbstractionConstructor : IsolatorAbstract
{
    public override bool ExecuteAbstract()
    {
        return ResultInterface && !this.IsContextDefault();
    }
}

//[Isolator(" ")] // This ignore isolation in this class
public abstract class IsolatorAbstract : IsolatorInterface
{
    public IsolatorAbstract()
    {
        Result = true;
    }
    protected bool Result { get; set; }
    protected bool ResultInterface { get; set; }
    public abstract bool ExecuteAbstract();
    public bool Execute()
    {
        ResultInterface = true;
        return ExecuteAbstract();
    }
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
        return Result && !System.Reflection.Assembly.GetExecutingAssembly().IsContextDefault();
    }
}

public interface IsolatorInterface
{
    bool Execute();
}