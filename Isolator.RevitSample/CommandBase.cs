using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Isolator.RevitSample
{
    [Transaction(TransactionMode.Manual)]
    public class CommandBase : ExternalCommand
    {
        public override void Execute()
        {
            UIApplication uiapp = Application;
        }
    }
}
