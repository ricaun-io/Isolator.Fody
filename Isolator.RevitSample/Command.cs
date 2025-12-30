using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Isolator.RevitSample
{
    [Isolator]
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            UIApplication uiapp = commandData.Application;

            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public bool Execute2(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            UIApplication uiapp = commandData.Application;

            System.Console.WriteLine("Test");

            return true;
        }
        public void Execute3(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            System.Console.WriteLine("Test");
        }
        public object Execute4(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            return null;
        }

        public Result Execute(ExternalCommandData commandData, ElementSet elementSet)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result Execute(ExternalCommandData commandData, ref string message)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result Execute(ref string message, ElementSet elementSet)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result Execute(ref string message)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result Execute(ExternalCommandData commandData)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
    }

}
