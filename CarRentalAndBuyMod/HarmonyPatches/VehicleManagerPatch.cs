using CarRentalAndBuyMod.AI;
using ColossalFramework;
using HarmonyLib;

namespace CarRentalAndBuyMod.CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleManagerPatch
    {
        [HarmonyPatch(typeof(VehicleManager), "ReleaseVehicle")]
        [HarmonyPrefix]
        public static bool ReleaseVehicle(ushort vehicle)
        {
            ushort source_building = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicle].m_sourceBuilding;
            BuildingManager instance = Singleton<BuildingManager>.instance;
            Building building = instance.m_buildings.m_buffer[source_building];
            if (source_building != 0 && building.Info.GetAI() is CarRentalAI)
            {
                return false;
            }
            return true;
        }
    }
}
