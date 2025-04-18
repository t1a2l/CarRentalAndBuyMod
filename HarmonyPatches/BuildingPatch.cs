using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
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
            if(!VehicleRentalManager.RentalDataExist(data.m_citizen))
            {
                return true;
            }
            var rental = VehicleRentalManager.GetRentalData(data.m_citizen);
            var sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding];
            if (sourceBuilding.Info.GetAI() is CarRentalAI && rental.CarRentalBuildingID == data.m_sourceBuilding)
            {
                return false;
            }
            return true;
        }
    }
}
