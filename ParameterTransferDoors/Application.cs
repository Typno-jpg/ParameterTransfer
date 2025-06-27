
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using ParameterTransferDoors.Commands;
using ParameterTransferDoors.Helpers;
using ParameterTransferDoors.Updater;
using Autodesk.Revit.DB.Events;
using System.Windows.Media.Imaging;

namespace ParameterTransferDoors
{
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon();

            // Updater aktivieren
            bool autoUpdate = true; // Standardmäßig aktiviert
            UpdaterController.ActivateUpdater(UiApplication, autoUpdate);

            // Event registrieren: Parameter erst nach Projektstart prüfen
            //UiApplication.Application.DocumentOpened += OnDocumentOpened;
        }

        //public override void OnShutdown()
        //{
        //    // Event abmelden
        //    //UiApplication.Application.DocumentOpened -= OnDocumentOpened;
        //}

        private void CreateRibbon()
        {
            const string tabName = "PT";
            const string panelNameDoors = "Türen";
            const string panelNameRooms = "Räume";

            try
            {
                UiApplication.CreateRibbonTab(tabName);
            }
            catch { }

            // Türen-Panel
            var panelDoors = UiApplication.CreateRibbonPanel(tabName, panelNameDoors);

            panelDoors.AddPushButton<AddSharedParametersCommand>("Parameterhinzufügen")
                .SetImage("/ParameterTransferDoors;component/Resources/Icons/ParameterIcon16.png")
                .SetLargeImage("/ParameterTransferDoors;component/Resources/Icons/ParameterIcon32.png");

            panelDoors.AddPushButton<ToggleUpdaterCommand>("Updaterumschalten");
            panelDoors.AddPushButton<UpdateDoorsCommand>("Alle Türenaktualisieren");
            panelDoors.AddPushButton<UpdateSelectedDoorsCommand>("Auswahlaktualisieren");
            panelDoors.AddPushButton<ConfigureDoorNumbersCommand>("Türnummernkonfigurieren");

            // Räume-Panel
            var panelRooms = UiApplication.CreateRibbonPanel(tabName, panelNameRooms);

            panelRooms.AddPushButton<ParameterTransferRooms.Commands.AddSharedParametersCommandRooms>("Parameterhinzufügen");
            panelRooms.AddPushButton<CalculateLichteHoeheCommand>("Lichte Höhe berechnen");
        }

        private void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            var doc = e.Document;
            if (doc.IsFamilyDocument) return; // Nur bei Projektdateien

            SharedParameterHelper.EnsureSharedParameters(doc);
        }
    }
}
