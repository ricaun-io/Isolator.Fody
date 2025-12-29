
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var i1 = new IsolatorClass("Test1");
        var i2 = new IsolatorClass("Test2");
        var i3 = new IsolatorClass("Test3");

        i1.Name = "Hi";
        //i1.Context = "Context1";

        Console.WriteLine(i1);
        Console.WriteLine(i2);
        Console.WriteLine(i3);

        Console.WriteLine(IsolatorStaticClass.MyStaticMethod(21));

        IsolatorStaticClass.MyStaticMethod("Test");
        IsolatorStaticClass.MyStaticMethod("Test", 2);


        //object data = new ExternalCommand();
        //var method = data.GetType().GetMethod("Execute", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, new Type[1] { typeof(ref string).MakeByRefType() }, null);

        //var cmd = "";
        //new ExternalCommand().Execute(ref cmd);

        //Console.WriteLine(cmd);

        //new MyClass().MyMethod();
        //Console.WriteLine(" ");
        //new MyClass2().MyMethod();
        //var obj = new MyClass2("class name");
        //var arg = obj.MyMethod2("argument");
        //Console.WriteLine(arg);
    }
}

[Isolator]
public static class IsolatorStaticClass
{
    static IsolatorStaticClass()
    {
        Console.WriteLine("Inside IsolatorStaticClass static constructor");
    }
    public static void MyStaticMethod()
    {
        Console.WriteLine("Inside MyStaticMethod");
    }

    public static int MyStaticMethod(int x)
    {
        return x * 2;
    }

    public static void MyStaticMethod(params object[] args)
    {
        foreach (var item in args)
        {
            Console.WriteLine(item);
        }
    }
}

[Isolator]
public class IsolatorClass
{
    public IsolatorClass(string name = null)
    {
        var context = AssemblyLoadContext.GetLoadContext(typeof(IsolatorClass).Assembly)?.ToString();
        Console.WriteLine(context);
        this.Name = name;
    }

    private void PrivateMethod()
    {
        Console.WriteLine($"Inside PrivateMethod {this}");
    }

    public string Name { get; set; }

    internal string Context { get; set; }

    override public string ToString()
    {
        return $"IsolatorClass: {Name} in {AssemblyLoadContext.GetLoadContext(typeof(IsolatorClass).Assembly)}";
    }
}

[Isolator]
public class MyClass
{
    public void MyMethod()
    {
        Console.WriteLine($"Inside MyMethod {this}");
    }
}

[Isolator]
public class MyClass2
{
    public MyClass2()
    {
        Console.WriteLine($"Inside MyClass2 constructor {this}");
    }
    public MyClass2(string name)
    {
        Console.WriteLine($"Inside MyClass2 constructor {this} {name}");
    }
    public void MyMethod()
    {
        Console.WriteLine($"Inside MyMethod {this}");
    }

    public object MyMethod2(object arg)
    {
        Console.WriteLine($"Inside MyMethod2 {this}");
        return arg;
    }
}