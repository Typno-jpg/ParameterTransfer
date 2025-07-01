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
                    warnings.Add($"{room.Name} (keine gültige Position)");
                    continue;
                }

                var lichteHoeheParam = room.LookupParameter("KAI_GEO_Lichte_Höhe");
                var okFfbParam = room.LookupParameter("KAI_RAU_OK_FFB");
                var ukFdParam = room.LookupParameter("KAI_RAU_UK_FD");

                lichteHoeheParam?.Set(0);
                okFfbParam?.Set("");
                ukFdParam?.Set("");

                double baseOffset = room.get_Parameter(BuiltInParameter.ROOM_LOWER_OFFSET)?.AsDouble() ?? 0;
                double rayStartZ = room.Level.Elevation + baseOffset;

                var formatOptions = new FormatValueOptions();
                string ffbText = UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, rayStartZ, false, formatOptions);
                okFfbParam?.Set(ffbText);

                XYZ rayStart = new XYZ(locationPoint.Point.X, locationPoint.Point.Y, rayStartZ);
                XYZ rayDirection = new XYZ(0, 0, 1);

                double upperLimitElevation = 0;
                Parameter upperLimitParam = room.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL);
                Parameter limitOffsetParam = room.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET);
                double upperOffset = limitOffsetParam?.AsDouble() ?? 0;

                if (upperLimitParam != null && upperLimitParam.AsElementId() != ElementId.InvalidElementId)
                {
                    Level upperLevel = doc.GetElement(upperLimitParam.AsElementId()) as Level;
                    if (upperLevel != null)
                    {
                        upperLimitElevation = upperLevel.Elevation + upperOffset;
                    }
                }
                else
                {
                    upperLimitElevation = room.Level.Elevation + room.UnboundedHeight;
                }

                double maxDistanceMeters = (upperLimitElevation - rayStartZ) + 1.0;
                double maxDistance = UnitUtils.ConvertToInternalUnits(maxDistanceMeters, UnitTypeId.Meters);

                ReferenceIntersector referenceIntersector = new ReferenceIntersector(
                    new ElementClassFilter(typeof(Ceiling)),
                    FindReferenceTarget.Element,
                    view3D
                );

                var hits = referenceIntersector.Find(rayStart, rayDirection)
                    .Where(r => r.Proximity <= maxDistance)
                    .OrderBy(r => r.Proximity)
                    .ToList();

                ReferenceWithContext nearest = hits.FirstOrDefault();
                double lichteHoehe;

                if (nearest != null)
                {
                    Reference reference = nearest.GetReference();
                    Element ceiling = doc.GetElement(reference.ElementId);
                    BoundingBoxXYZ ceilingBoundingBox = ceiling.get_BoundingBox(view3D);

                    if (ceilingBoundingBox != null)
                    {
                        double ceilingBottomZ = ceilingBoundingBox.Min.Z;
                        lichteHoehe = ceilingBottomZ - rayStart.Z;
                        string fdText = UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, ceilingBottomZ, false, formatOptions);
                        ukFdParam?.Set(fdText);
                    }
                    else
                    {
                        lichteHoehe = upperLimitElevation - rayStart.Z;
                    }
                }
                else
                {
                    lichteHoehe = upperLimitElevation - rayStart.Z;
                }

                if (lichteHoeheParam != null && !lichteHoeheParam.IsReadOnly && lichteHoehe > 0)
                {
                    lichteHoeheParam.Set(lichteHoehe);
                }
                else if (lichteHoehe <= 0)
                {
                    warnings.Add($"{room.Name} (lichte Höhe = 0)");
                }
            }

            t.Commit();
        }

        string summary = "Lichte Höhen wurden berechnet und eingetragen.";
        if (warnings.Count > 0)
        {
            summary += "\n\nFolgende Räume wurden übersprungen oder sind auffällig:\n" + string.Join("\n", warnings);
        }

        TaskDialog.Show("Fertig", summary);
        return Result.Succeeded;
    }
}
