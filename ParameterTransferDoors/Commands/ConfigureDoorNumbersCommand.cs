using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ParameterTransferDoors.Helpers;
using ParameterTransferDoors.Models;
using ParameterTransferDoors.UI;

namespace ParameterTransferDoors.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ConfigureDoorNumbersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIApplication uiApp = commandData.Application;

            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Nicht unterstützt", "Diese Funktion ist nur in Projektdateien verfügbar.");
                return Result.Cancelled;
            }

            var window = new DoorNumberOptionsWindow(doc, uiApp);
            if (window.ShowDialog() != true)
                return Result.Cancelled;

            var options = window.SelectedOptions;

            using (Transaction tx = new Transaction(doc, "Türnummern aktualisieren"))
            {
                tx.Start();

                var collector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType();

                foreach (Element element in collector)
                {
                    if (element is FamilyInstance door)
                    {
                        Room fromRoom = door.FromRoom;
                        Room toRoom = door.ToRoom;

                        string fromName = fromRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                        string fromNumber = fromRoom?.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                        string toName = toRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                        string toNumber = toRoom?.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                        SharedParameterHelper.UpdateDoorParameters(door, fromName, fromNumber, toName, toNumber, options);
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("Fertig", "Türnummern wurden aktualisiert.");
            return Result.Succeeded;
        }
    }
}
