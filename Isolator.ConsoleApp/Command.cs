[Isolator]
public class Command : CommandBase
{
    public Command() : base()
    {
        Console.WriteLine("Command");
    }
    public override void Execute()
    {
        Console.WriteLine("Inside Command Execute");
    }
}

public abstract class CommandBase
{
    public CommandBase()
    {
        Console.WriteLine("CommandBase");
    }
    public abstract void Execute();
}
