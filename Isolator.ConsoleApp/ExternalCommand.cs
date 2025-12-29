[Isolator]
public class ExternalCommand// : IExternalCommand
{
    public void Execute(ref string message)
    {
        Console.WriteLine($"Execute < {message}");
        message = "Executed MyExternalCommand";
        Console.WriteLine($"Execute > {message}");
    }

    //public object Execute(object context)
    //{
    //    Console.WriteLine("MyExternalCommand executed with context: " + context);
    //    return null;
    //}

    public object Execute(object context, ref string message, params object[] args)
    {
        message = "Executed MyExternalCommand";
        Console.WriteLine("MyExternalCommand executed with context: " + context);
        return true;
    }
}

//public interface IExternalCommand
//{
//    object Execute(object context, ref string message, params object[] args);
//}
