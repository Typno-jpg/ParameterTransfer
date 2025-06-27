using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace ParameterTransferDoors.Updater
{
    public class DoorUpdater : IUpdater
    {
        private readonly UpdaterId _updaterId;

        public DoorUpdater(AddInId addInId)
        {
            _updaterId = new UpdaterId(addInId, new Guid("A1B2C3D4-E5F6-7890-1234-56789ABCDEF0"));
        }

        public void Execute(UpdaterData data)
        {
            if (!UpdaterController.IsActive) return;

            Document doc = data.GetDocument();

            var elementIds = new List<ElementId>();
            elementIds.AddRange(data.GetModifiedElementIds());
            elementIds.AddRange(data.GetAddedElementIds());

            foreach (var id in elementIds)
            {
                Element element = doc.GetElement(id);
                if (element is FamilyInstance door && door.Category.Id.Value == (int)BuiltInCategory.OST_Doors)
                {
                    Room fromRoom = door.FromRoom;
                    Room toRoom = door.ToRoom;

                    string fromName = fromRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                    string fromNumber = fromRoom?.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                    string toName = toRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                    string toNumber = toRoom?.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                    SetStringParameter(door, "AusRaum_Name", fromName);
                    SetStringParameter(door, "AusRaum_Nummer", fromNumber);
                    SetStringParameter(door, "InRaum_Name", toName);
                    SetStringParameter(door, "InRaum_Nummer", toNumber);
                }

            }
        }

        private void SetStringParameter(Element element, string paramName, string value)
        {
            Parameter param = element.LookupParameter(paramName);
            if (param != null && param.StorageType == StorageType.String && !param.IsReadOnly)
            {
                param.Set(value);
            }
        }






        public void RegisterUpdater(UIApplication app)
        {
            if (!UpdaterRegistry.IsUpdaterRegistered(_updaterId))
            {
                UpdaterRegistry.RegisterUpdater(this);

                ElementCategoryFilter doorFilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);

                // Reagiere auf jegliche Änderungen
                UpdaterRegistry.AddTrigger(_updaterId, doorFilter, Element.GetChangeTypeAny());

                // Reagiere zusätzlich auf neu platzierte Türen
                UpdaterRegistry.AddTrigger(_updaterId, doorFilter, Element.GetChangeTypeElementAddition());
            }
        }



        public void UnregisterUpdater()
        {
            if (UpdaterRegistry.IsUpdaterRegistered(_updaterId))
            {
                UpdaterRegistry.UnregisterUpdater(_updaterId);
            }
        }

        public UpdaterId GetUpdaterId() => _updaterId;

        public string GetUpdaterName() => "Door Parameter Updater";

        public string GetAdditionalInformation() => "Aktualisiert InRaum und AusRaum Parameter bei Türänderungen.";

        public ChangePriority GetChangePriority() => ChangePriority.RoomsSpacesZones;

    }
}
