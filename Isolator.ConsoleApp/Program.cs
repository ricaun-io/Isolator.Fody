
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

        //new MyClass().MyMethod();
        //Console.WriteLine(" ");
        //new MyClass2().MyMethod();
        //var obj = new MyClass2("class name");
        //var arg = obj.MyMethod2("argument");
        //Console.WriteLine(arg);
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
