using CarRentalAndBuyMod.AI;
using ColossalFramework;
using HarmonyLib;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleManagerPatch
    {
        [HarmonyPatch(typeof(VehicleManager), "ReleaseVehicle")]
        [HarmonyPrefix]
        public static void ReleaseVehicle(ushort vehicle)
        {
            var rented_vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicle];
            if (rented_vehicle.m_sourceBuilding != 0 && rented_vehicle.Info.GetAI() is PassengerCarAI)
            {
                ref var sourceBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[rented_vehicle.m_sourceBuilding];
                if (sourceBuilding.Info.GetAI() is CarRentalAI carRentalAI && (rented_vehicle.m_flags & Vehicle.Flags.Spawned) != 0)
                {
                    carRentalAI.m_rentedCarCount--;
                }
            }
        }
    }
}
