namespace ParameterTransferDoors.Models
{
    public class DoorNumberOptions
    {
        public string ZusatzparameterName { get; set; } = "";
        public string Trennzeichen { get; set; } = "-";
        public string TürnummerParameterName { get; set; } = "KAI_TUE_Nummer";
        public bool AutoUpdate { get; set; } = true; // Standardmäßig aktiviert
    }
}
