[Isolator]
public class ExternalCommand// : IExternalCommand
{
    public void Execute(ref string message)
    {
        message = "Executed MyExternalCommand";
    }

    //public object Execute(object context)
    //{
    //    Console.WriteLine("MyExternalCommand executed with context: " + context);
    //    return null;
    //}

    //public object Execute(object context, ref string message, params object[] args)
    //{
    //    message = "Executed MyExternalCommand";
    //    Console.WriteLine("MyExternalCommand executed with context: " + context);
    //    return true;
    //}
}

//public interface IExternalCommand
//{
//    object Execute(object context, ref string message, params object[] args);
//}
