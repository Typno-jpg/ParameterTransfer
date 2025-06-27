using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Architecture;
using ParameterTransferDoors.Models;

namespace ParameterTransferDoors.Helpers
{
    public static class SharedParameterHelper
    {
        public static void EnsureSharedParameters(Document doc)
        {
            var app = doc.Application;

            string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string sharedParamFile = Path.Combine(basePath, "Resources", "SharedParameter", "KAI_PT.txt");

            if (!File.Exists(sharedParamFile))
            {
                TaskDialog.Show("Fehler", $"Shared Parameter Datei nicht gefunden:\n{sharedParamFile}");
                return;
            }

            app.SharedParametersFilename = sharedParamFile;
            DefinitionFile defFile = app.OpenSharedParameterFile();
            if (defFile == null)
            {
                TaskDialog.Show("Fehler", "Shared Parameter Datei konnte nicht geöffnet werden.");
                return;
            }

            var paramNames = new[]
            {
                "InRaum_Name", "InRaum_Nummer",
                "AusRaum_Name", "AusRaum_Nummer",
                "KAI_TUE_Nummer"
            };

            var category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Doors);
            var catSet = app.Create.NewCategorySet();
            catSet.Insert(category);

            var addedParams = new List<string>();

            foreach (var name in paramNames)
            {
                if (!ProjectParameterExists(doc, name))
                {
                    var definition = defFile.Groups
                        .SelectMany(g => g.Definitions)
                        .FirstOrDefault(d => d.Name == name);

                    if (definition == null) continue;

                    using (Transaction t = new Transaction(doc, $"Add Shared Parameter: {name}"))
                    {
                        t.Start();
                        var binding = app.Create.NewInstanceBinding(catSet);
                        doc.ParameterBindings.Insert(definition, binding, GroupTypeId.IdentityData);
                        t.Commit();
                        addedParams.Add(name);
                    }
                }
            }

            if (addedParams.Any())
            {
                string message = "Folgende Parameter wurden erfolgreich hinzugefügt:\n\n" +
                                 string.Join("\n", addedParams.Select(p => $"• {p}")) +
                                 $"\n\nQuelle:\n{sharedParamFile}";
                TaskDialog.Show("Parameter hinzugefügt", message);
            }
            else
            {
                TaskDialog.Show("Parameterprüfung", "Alle benötigten Türparameter sind bereits im Projekt vorhanden.");
            }
        }

        private static bool ProjectParameterExists(Document doc, string paramName)
        {
            BindingMap bindings = doc.ParameterBindings;
            DefinitionBindingMapIterator iter = bindings.ForwardIterator();

            while (iter.MoveNext())
            {
                Definition? definition = iter.Key;
                if (definition == null || definition.Name != paramName)
                    continue;

                ElementBinding? binding = iter.Current as ElementBinding;
                if (binding?.Categories == null)
                    continue;

                foreach (Category cat in binding.Categories)
                {
                    if (cat.Id.Value == (int)BuiltInCategory.OST_Doors)
                        return true;
                }
            }

            return false;
        }

        private static void SetStringParameter(Element element, string paramName, string value)
        {
            Parameter param = element.LookupParameter(paramName);
            if (param != null && param.StorageType == StorageType.String && !param.IsReadOnly)
            {
                param.Set(value);
            }
        }

        public static void UpdateAllDoors(Document doc, DoorNumberOptions options)
        {
            using (Transaction tx = new Transaction(doc, "Türen aktualisieren"))
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

                        UpdateDoorParameters(door, fromName, fromNumber, toName, toNumber, options);
                    }
                }

                tx.Commit();
            }
        }

        public static void UpdateDoorParameters(Element door, string fromName, string fromNumber, string toName, string toNumber, DoorNumberOptions options)
        {
            SetStringParameter(door, "AusRaum_Name", fromName);
            SetStringParameter(door, "AusRaum_Nummer", fromNumber);
            SetStringParameter(door, "InRaum_Name", toName);
            SetStringParameter(door, "InRaum_Nummer", toNumber);

            string basis = !string.IsNullOrWhiteSpace(toNumber) ? toNumber : fromNumber;

            string zusatz = "";
            if (!string.IsNullOrWhiteSpace(options.ZusatzparameterName))
            {
                var param = door.LookupParameter(options.ZusatzparameterName);
                if (param != null && param.StorageType == StorageType.String)
                {
                    zusatz = param.AsString();
                }
            }

            string nummer = basis;
            if (!string.IsNullOrWhiteSpace(zusatz))
            {
                nummer += options.Trennzeichen + zusatz;
            }

            // Verwende den ausgewählten Parameter für die Türnummer
            SetStringParameter(door, options.TürnummerParameterName, nummer);
        }

    }
}
