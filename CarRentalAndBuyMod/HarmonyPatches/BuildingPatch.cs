using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using HarmonyLib;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class BuildingPatch
    {
        [HarmonyPatch(typeof(Building), "RemoveSourceCitizen")]
        [HarmonyPrefix]
        public static bool RemoveSourceCitizen(ushort instanceID, ref CitizenInstance data)
        {
            var rental = VehicleRentalManager.GetVehicleRental(data.m_citizen);
            var sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding];
            if (!rental.Equals(default(VehicleRentalManager.Rental)) && sourceBuilding.Info.GetAI() is CarRentalAI && rental.CarRentalBuildingID == data.m_sourceBuilding)
            {
                return false;
            }
            return true;
        }
    }
}
