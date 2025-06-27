using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ParameterTransferDoors.Models;

namespace ParameterTransferDoors.UI
{
    public partial class DoorNumberOptionsWindow : Window
    {
        private readonly Document _doc;
        private readonly UIApplication _uiApp;

        public DoorNumberOptions SelectedOptions { get; private set; }
        public static DoorNumberOptions LetzteOptionen { get; private set; } = new DoorNumberOptions();

        public DoorNumberOptionsWindow(Document doc, UIApplication uiApp)
        {
            InitializeComponent();
            _doc = doc;
            _uiApp = uiApp;
            SelectedOptions = new DoorNumberOptions(); // Initialisierung für Nullable-Sicherheit
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDoorParameters();

            // Letzte Werte setzen
            TrennzeichenBox.Text = LetzteOptionen.Trennzeichen;

            if (!string.IsNullOrEmpty(LetzteOptionen.ZusatzparameterName) &&
                ParameterComboBox.Items.Contains(LetzteOptionen.ZusatzparameterName))
            {
                ParameterComboBox.SelectedItem = LetzteOptionen.ZusatzparameterName;
            }

            // Lade die Parameter für die Türnummer
            LoadDoorNumberParameters();

            // Setze den Status der Checkbox
            AutoUpdateCheckBox.IsChecked = LetzteOptionen.AutoUpdate;

            UpdatePreview();
        }

        private void LoadDoorParameters()
        {
            var door = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .OfType<FamilyInstance>()
                .FirstOrDefault();

            if (door == null) return;

            var parameters = door.Parameters
                .Cast<Parameter>()
                .Where(p => p.StorageType == StorageType.String && !p.IsReadOnly)
                .Select(p => p.Definition.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            ParameterComboBox.ItemsSource = parameters;

            if (parameters.Contains("TürIndex"))
                ParameterComboBox.SelectedItem = "TürIndex";
            else
                ParameterComboBox.SelectedIndex = 0;
        }

        private void LoadDoorNumberParameters()
        {
            var door = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .OfType<FamilyInstance>()
                .FirstOrDefault();

            if (door == null) return;

            var parameters = door.Parameters
                .Cast<Parameter>()
                .Where(p => p.StorageType == StorageType.String && !p.IsReadOnly)
                .Select(p => p.Definition.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            DoorNumberParameterComboBox.ItemsSource = parameters;

            // Setze den Standardwert auf "KAI_TUE_Nummer" oder den zuletzt eingestellten Parameter
            if (!string.IsNullOrEmpty(LetzteOptionen.TürnummerParameterName) && parameters.Contains(LetzteOptionen.TürnummerParameterName))
            {
                DoorNumberParameterComboBox.SelectedItem = LetzteOptionen.TürnummerParameterName;
            }
            else if (parameters.Contains("KAI_TUE_Nummer"))
            {
                DoorNumberParameterComboBox.SelectedItem = "KAI_TUE_Nummer";
            }
            else
            {
                DoorNumberParameterComboBox.SelectedIndex = 0;
            }
        }

        private void UpdatePreview()
        {
            var door = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .OfType<FamilyInstance>()
                .FirstOrDefault();

            if (door == null)
            {
                PreviewText.Text = "(Keine Tür gefunden)";
                return;
            }

            Room toRoom = door.ToRoom;
            string inRaumNummer = toRoom?.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

            string zusatz = "";
            string paramName = ParameterComboBox.SelectedItem?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(paramName))
            {
                var param = door.LookupParameter(paramName);
                if (param != null && param.StorageType == StorageType.String)
                {
                    zusatz = param.AsString();
                }
            }

            string trennzeichen = TrennzeichenBox.Text ?? "";
            string vorschau = inRaumNummer;

            if (!string.IsNullOrWhiteSpace(zusatz))
            {
                vorschau += trennzeichen + zusatz;
            }

            PreviewText.Text = vorschau;
        }

        private void UpdatePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void ParameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void DoorNumberParameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedOptions = new DoorNumberOptions
            {
                ZusatzparameterName = ParameterComboBox.SelectedItem?.ToString() ?? "",
                Trennzeichen = TrennzeichenBox.Text ?? "",
                TürnummerParameterName = DoorNumberParameterComboBox.SelectedItem?.ToString() ?? "KAI_TUE_Nummer",
                AutoUpdate = AutoUpdateCheckBox.IsChecked ?? false
            };

            LetzteOptionen = SelectedOptions; // <-- speichern

            // Updater aktivieren oder deaktivieren basierend auf der Checkbox
            UpdaterController.ActivateUpdater(_uiApp, SelectedOptions.AutoUpdate);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
