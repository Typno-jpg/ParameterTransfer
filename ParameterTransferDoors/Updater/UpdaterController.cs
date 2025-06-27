using Autodesk.Revit.UI;
using ParameterTransferDoors.Updater;

namespace ParameterTransferDoors
{
    public static class UpdaterController
    {
        private static bool _isActive = true;
        private static DoorUpdater? _updater;

        public static bool IsActive => _isActive;

        public static bool ToggleUpdater(UIApplication app)
        {
            _isActive = !_isActive;

            if (_updater == null)
            {
                _updater = new DoorUpdater(app.ActiveAddInId);
            }

            if (_isActive)
            {
                _updater.RegisterUpdater(app);
            }
            else
            {
                _updater.UnregisterUpdater();
            }

            return _isActive;
        }

        public static void ActivateUpdater(UIApplication app, bool autoUpdate)
        {
            _isActive = autoUpdate;

            if (_updater == null)
            {
                _updater = new DoorUpdater(app.ActiveAddInId);
            }

            if (_isActive)
            {
                _updater.RegisterUpdater(app);
            }
            else
            {
                _updater.UnregisterUpdater();
            }
        }

    }
}
