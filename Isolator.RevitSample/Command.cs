using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Reflection;

namespace Isolator.RevitSample
{
    [Isolator]
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Command(out string message)
        {
            message = "Initialized";
        }

        public Command(out string message, ElementSet elementSet)
        {
            message = "Initialized";
        }

        public Command(out string message, ElementSet elementSet, UIApplication uiapp)
        {
            message = "Initialized";
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            UIApplication uiapp = commandData.Application;

            System.Console.WriteLine(uiapp.Application.VersionBuild);
            System.Console.WriteLine("Test");

#if NET
            var assembly = Assembly.GetExecutingAssembly();
            var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
            System.Console.WriteLine(context);
#endif

            return Result.Succeeded;
        }

        public Result Execute7(ref string message, ExternalCommandData commandData,  ElementSet elementSet)
        {
            UIApplication uiapp = commandData.Application;

            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }

        public Result Execute8( ExternalCommandData commandData, ElementSet elementSet, ref string message)
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

        public Result AExecute(ExternalCommandData commandData, ElementSet elementSet)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result BExecute(ExternalCommandData commandData, ref string message)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result CExecute(ref string message, ElementSet elementSet)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result DExecute(ref string message)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
        public Result EExecute(ExternalCommandData commandData)
        {
            System.Console.WriteLine("Test");

            return Result.Succeeded;
        }
    }

}
