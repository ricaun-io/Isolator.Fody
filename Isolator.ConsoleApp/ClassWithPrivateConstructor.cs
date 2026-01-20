namespace Isolator.ConsoleApp;

public class ClassWithPrivateConstructor : IsolatorInterface
{
    private bool Result { get; set; }
    private ClassWithPrivateConstructor()
    {
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
    public ClassWithAbstractionConstructor() : base(true)
    {
    }
    public override bool ExecuteAbstract()
    {
        ResultAbstract = Result && ResultInterface && !this.IsContextDefault();
        return ResultAbstract;
    }
}

//[Isolator(" ")] // This ignore isolation in this class
public abstract class IsolatorAbstract : IsolatorInterface
{
    public IsolatorAbstract()
    {
        Result = true;
        ResultOnlySet = true;
        ResultOnlyGet = true;
    }
    public IsolatorAbstract(bool value)
    {
        Result = value;
        ResultOnlySet = value;
        ResultOnlyGet = value;
    }
    public bool Result { get; set; }
    public bool ResultOnlySet { private get; set; }
    public bool ResultOnlyGet { get; }
    public bool ResultOnlyGetInterface => ResultInterface;
    public static bool ResultStatic { get; set; }
    public bool ResultAbstract { get; protected set; }
    public bool ResultInterface { get; private set; }
    public abstract bool ExecuteAbstract();
    public bool Execute()
    {
        ResultInterface = true;
        ResultStatic = true;

        if (!ResultOnlySet)
            throw new InvalidOperationException("ResultOnlySet was not set properly.");

        if (!ResultOnlyGet)
            throw new InvalidOperationException("ResultOnlyGet was not set properly.");

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