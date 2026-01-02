
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

public class Program
{
    public unsafe static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var i1 = new IsolatorClass("Test1");
        var i2 = new IsolatorClass("Test2");
        var i3 = new IsolatorClass("Test3");

        i1.Name = "Hi";
        Debug.Assert(i1.Name == "Hi");
        //i1.Context = "Context1";

        Console.WriteLine(i1);
        Console.WriteLine(i2);
        Console.WriteLine(i3);

        Console.WriteLine(IsolatorStaticClass.MyStaticMethod(21));
        Console.WriteLine(IsolatorStaticClass.MyStaticMethod(21, 2));

        IsolatorStaticClass.MyStaticMethod("Test");
        IsolatorStaticClass.MyStaticMethod("Test", 2);

        //var message = "?";
        //NewMethod(ref message);
        //Console.WriteLine(message);

        var cmd = "?";
        new ExternalCommand().Execute(ref cmd);
        Console.WriteLine(cmd);

        // Test if we can access base constructor
        new Command().Execute();

        // Test if we can access private constructor
        (Activator.CreateInstance(typeof(CommandPrivate), true) as CommandPrivate).Execute();

        //new MyClass().MyMethod();
        //Console.WriteLine(" ");
        //new MyClass2().MyMethod();
        //var obj = new MyClass2("class name");
        //var arg = obj.MyMethod2("argument");
        //Console.WriteLine(arg);
    }

    private static unsafe void NewMethod(ref string message)
    {
        object data = new ExternalCommand();
        var method = data.GetType().GetMethod("Execute", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, new Type[1] { typeof(string).MakeByRefType() }, null);
        var args = new object[1] { message };
        method.Invoke(data, args);
        Console.WriteLine($"{message} >>>");
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

    public static int MyStaticMethod(int x, int y)
    {
        return x * y;
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
    public IsolatorClass(ref string name)
    {

    }

    public IsolatorClass(string name = null)
    {
        var context = AssemblyLoadContext.GetLoadContext(typeof(IsolatorClass).Assembly)?.ToString();
        Console.WriteLine(context);
        this.Name = name;
    }

    public IsolatorClass(string name, string name2)
    {
        var context = AssemblyLoadContext.GetLoadContext(typeof(IsolatorClass).Assembly)?.ToString();
        Console.WriteLine(context);
        this.Name = name;
    }

    public int MyStaticMethod(out string x, string y)
    {
        x = "output";
        return 0;
    }

    public int MyStaticMethod(string x, string y)
    {
        return 0;
    }

    public int MyStaticMethod(string x, string y, string z)
    {
        return 0;
    }

    public int MyStaticMethod(string x, string y, int z)
    {
        return 0;
    }

    public int MyStaticMethod(string x)
    {
        return 0;
    }

    public int MyStaticMethod(int x)
    {
        return 0;
    }

    public int MyStaticMethod(int x, int y)
    {
        return 0;
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