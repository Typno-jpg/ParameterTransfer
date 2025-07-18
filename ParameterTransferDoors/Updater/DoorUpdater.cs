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

                // Fall 1: Tür wurde geändert oder hinzugefügt
                if (element is FamilyInstance door && door.Category.Id.Value == (int)BuiltInCategory.OST_Doors)
                {
                    // Raumdaten aktualisieren
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

                    // Host-Material übertragen
                    Element? host = door.Host != null ? doc.GetElement(door.Host.Id) : null;
                    if (host != null && host.Category.Id.Value == (int)BuiltInCategory.OST_Walls)
                    {
                        Element wallType = doc.GetElement(host.GetTypeId());
                        Parameter wallMaterialParam = wallType?.LookupParameter("KAI_MAT_Material");

                        if (wallMaterialParam != null && wallMaterialParam.StorageType == StorageType.String)
                        {
                            string materialValue = wallMaterialParam.AsString() ?? "";
                            SetStringParameter(door, "KAI_MAT_Einbauort", materialValue);
                        }
                    }
                }

                // Fall 2: Wand wurde geändert → alle gehosteten Türen aktualisieren
                else if (element.Category?.Id.Value == (int)BuiltInCategory.OST_Walls)
                {
                    var wall = element;
                    var collector = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Doors)
                        .WhereElementIsNotElementType();


                    foreach (Element e in collector)
                    {
                        if (e is FamilyInstance hostedDoor && hostedDoor.Host?.Id == wall.Id)
                        {
                            Element wallType = doc.GetElement(wall.GetTypeId());
                            Parameter wallMaterialParam = wallType?.LookupParameter("KAI_MAT_Material");

                            if (wallMaterialParam != null && wallMaterialParam.StorageType == StorageType.String)
                            {
                                string materialValue = wallMaterialParam.AsString() ?? "";
                                SetStringParameter(hostedDoor, "KAI_MAT_Einbauort", materialValue);
                            }
                        }
                    }

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

                // Türen
                ElementCategoryFilter doorFilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
                UpdaterRegistry.AddTrigger(_updaterId, doorFilter, Element.GetChangeTypeAny());
                UpdaterRegistry.AddTrigger(_updaterId, doorFilter, Element.GetChangeTypeElementAddition());

                // Wände
                ElementCategoryFilter wallFilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
                UpdaterRegistry.AddTrigger(_updaterId, wallFilter, Element.GetChangeTypeAny());
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
        public string GetAdditionalInformation() => "Aktualisiert Raum- und Materialparameter bei Tür- und Wandänderungen.";
        public ChangePriority GetChangePriority() => ChangePriority.RoomsSpacesZones;
    }
}
