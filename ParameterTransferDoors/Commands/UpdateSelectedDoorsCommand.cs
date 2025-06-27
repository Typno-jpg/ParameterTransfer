using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ParameterTransferDoors.Helpers;
using ParameterTransferDoors.Models;
using System.Collections.Generic;
using System.Linq;

namespace ParameterTransferDoors.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class UpdateSelectedDoorsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Nicht unterstützt", "Diese Funktion ist nur in Projektdateien verfügbar.");
                return Result.Cancelled;
            }

            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("Keine Auswahl", "Bitte wähle eine oder mehrere Türen aus.");
                return Result.Cancelled;
            }

            var selectedDoors = selectedIds
                .Select(id => doc.GetElement(id))
                .OfType<FamilyInstance>()
                .Where(e => e.Category.Id.Value == (int)BuiltInCategory.OST_Doors)
                .ToList();

            if (!selectedDoors.Any())
            {
                TaskDialog.Show("Keine Türen", "Die Auswahl enthält keine Türen.");
                return Result.Cancelled;
            }

            using (Transaction tx = new Transaction(doc, "Ausgewählte Türen aktualisieren"))
            {
                tx.Start();

                foreach (var door in selectedDoors)
                {
                    Room fromRoom = door.FromRoom;
                    Room toRoom = door.ToRoom;

                    string fromName = fromRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                    string fromNumber = fromRoom?.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                    string toName = toRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                    string toNumber = toRoom?.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                    SharedParameterHelper.UpdateDoorParameters(door, fromName, fromNumber, toName, toNumber, new DoorNumberOptions());
                }

                tx.Commit();
            }

            TaskDialog.Show("Fertig", "Die ausgewählten Türen wurden aktualisiert.");
            return Result.Succeeded;
        }
    }
}
