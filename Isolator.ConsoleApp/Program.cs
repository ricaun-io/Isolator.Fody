
public class Program
{ 
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        new MyClass().MyMethod();
        Console.WriteLine(" ");
        new MyClass2().MyMethod();
        new MyClass2("class name").MyMethod();
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

    public object MyMethod2()
    {
        Console.WriteLine($"Inside MyMethod2 {this}");
        return null;
    }
}
