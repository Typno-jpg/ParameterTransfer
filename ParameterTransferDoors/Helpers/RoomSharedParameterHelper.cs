using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ParameterTransferRooms.Helpers
{
    public static class RoomSharedParameterHelper
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
                "KAI_GEO_Lichte_Höhe"
            };

            var category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Rooms);
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
                        doc.ParameterBindings.Insert(definition, binding, GroupTypeId.Length);
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
                TaskDialog.Show("Parameterprüfung", "Alle benötigten Raumparameter sind bereits im Projekt vorhanden.");
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
                    if (cat.Id.Value == (int)BuiltInCategory.OST_Rooms)
                        return true;
                }
            }

            return false;
        }
    }
}
