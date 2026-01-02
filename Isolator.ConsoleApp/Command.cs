[Isolator]
public class Command : CommandBase
{
    public override void Execute()
    {
        Console.WriteLine("Inside Command Execute");
    }
}

public abstract class CommandBase
{
    public abstract void Execute();
}
