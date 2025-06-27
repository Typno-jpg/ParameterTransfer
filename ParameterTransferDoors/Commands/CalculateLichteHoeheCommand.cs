using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

[TransactionAttribute(TransactionMode.Manual)]
public class CalculateLichteHoeheCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document doc = commandData.Application.ActiveUIDocument.Document;
        View3D view3D = doc.ActiveView as View3D;

        if (view3D == null)
        {
            TaskDialog.Show("Fehler", "Bitte öffnen Sie einen 3D-View, um diesen Befehl auszuführen.");
            return Result.Failed;
        }

        var rooms = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>();

        List<string> warnings = new List<string>();

        using (Transaction t = new Transaction(doc, "Lichte Höhe berechnen"))
        {
            t.Start();

            foreach (var room in rooms)
            {
                LocationPoint locationPoint = room.Location as LocationPoint;
                if (locationPoint == null)
                {
                    warnings.Add(room.Name);
                    continue;
                }

                XYZ rayStart = new XYZ(locationPoint.Point.X, locationPoint.Point.Y, room.get_BoundingBox(view3D)?.Min.Z ?? room.Level.Elevation);
                XYZ rayDirection = new XYZ(0, 0, 1);

                ReferenceIntersector referenceIntersector = new ReferenceIntersector(new ElementClassFilter(typeof(Ceiling)), FindReferenceTarget.Element, view3D);
                ReferenceWithContext refWithContext = referenceIntersector.FindNearest(rayStart, rayDirection);

                double lichteHoehe;
                if (refWithContext != null)
                {
                    Reference reference = refWithContext.GetReference();
                    Element ceiling = doc.GetElement(reference.ElementId);
                    BoundingBoxXYZ ceilingBoundingBox = ceiling.get_BoundingBox(view3D);
                    if (ceilingBoundingBox != null)
                    {
                        lichteHoehe = ceilingBoundingBox.Min.Z - rayStart.Z;
                    }
                    else
                    {
                        lichteHoehe = room.get_BoundingBox(view3D)?.Max.Z ?? (room.Level.Elevation + room.UnboundedHeight) - rayStart.Z;
                    }
                }
                else
                {
                    lichteHoehe = room.get_BoundingBox(view3D)?.Max.Z ?? (room.Level.Elevation + room.UnboundedHeight) - rayStart.Z;
                }

                var param = room.LookupParameter("KAI_GEO_Lichte_Höhe");
                if (param != null && !param.IsReadOnly)
                {
                    param.Set(lichteHoehe);
                }
            }

            t.Commit();
        }

        string summary = "Lichte Höhen wurden berechnet und eingetragen.";
        if (warnings.Count > 0)
        {
            summary += "\n\nFolgende Räume wurden übersprungen:\n" + string.Join("\n", warnings);
        }

        TaskDialog.Show("Fertig", summary);
        return Result.Succeeded;
    }
}
