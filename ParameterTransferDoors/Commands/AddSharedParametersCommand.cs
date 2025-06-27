using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ParameterTransferDoors.Helpers;

namespace ParameterTransferDoors.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AddSharedParametersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Nicht unterstützt", "Diese Funktion ist nur in Projektdateien verfügbar.");
                return Result.Cancelled;
            }

            SharedParameterHelper.EnsureSharedParameters(doc);
            return Result.Succeeded;
        }
    }
}