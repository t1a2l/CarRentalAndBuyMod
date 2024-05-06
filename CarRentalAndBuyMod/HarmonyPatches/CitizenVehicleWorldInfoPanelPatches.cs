using CarRentalAndBuyMod.Utils;
using HarmonyLib;


namespace CarRentalAndBuyMod.HarmonyPatches
{
    public static class CitizenVehicleWorldInfoPanelPatches
    {
        [HarmonyPatch(typeof(CitizenVehicleWorldInfoPanel), "UpdateBindings")]
        [HarmonyPostfix]
        private static void UpdateBindings(CitizenVehicleWorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
        {
            
        }
    }
}