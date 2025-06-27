using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ParameterTransferDoors.Helpers;
using ParameterTransferDoors.Models;

namespace ParameterTransferDoors.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class UpdateDoorsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Nicht unterstützt", "Diese Funktion ist nur in Projektdateien verfügbar.");
                return Result.Cancelled;
            }

            SharedParameterHelper.UpdateAllDoors(doc, new DoorNumberOptions());
            TaskDialog.Show("Fertig", "Alle Türen wurden erfolgreich aktualisiert.");
            return Result.Succeeded;
        }
    }
}
