using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace Isolator.RevitSample
{
    [Transaction(TransactionMode.Manual)]
    public abstract class ExternalCommand : IExternalCommand
    {
        public UIApplication Application { get; private set; }
        public abstract void Execute();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            Application = commandData.Application;
            try
            {
                Execute();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }
    }

}
