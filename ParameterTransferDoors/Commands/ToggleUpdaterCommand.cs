using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ParameterTransferDoors.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ToggleUpdaterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var status = UpdaterController.ToggleUpdater(commandData.Application);
            TaskDialog.Show("Updater Status", $"Automatische Aktualisierung ist jetzt {(status ? "aktiviert" : "pausiert")}.");
            return Result.Succeeded;
        }
    }
}

