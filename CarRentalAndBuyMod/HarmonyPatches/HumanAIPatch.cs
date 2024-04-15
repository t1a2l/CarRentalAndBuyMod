using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using HarmonyLib;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class HumanAIPatch
    {
        [HarmonyPatch(typeof(HumanAI), "ArriveAtDestination")]
        [HarmonyPrefix]
        public static bool ArriveAtDestination(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool success)
        {
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
            var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle];

            if (__instance.m_info.GetAI() is TouristAI touristAI && targetBuilding.Info.GetAI() is CarRentalAI carRentalAI)
            {
                // i am here to return the car and leave the city
                if (citizen.m_vehicle != 0 && vehicle.m_sourceBuilding == citizenData.m_targetBuilding)
                {
                    // get original outside connection target
                    var targeBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                    CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);

                    Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, 0, 0u);
                    Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle].m_sourceBuilding = 0;
                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].RemoveOwnVehicle(citizen.m_vehicle, ref vehicle);
                    vehicle.Unspawn(citizen.m_vehicle);

                    // move to outside connection
                    touristAI.StartMoving(citizenData.m_citizen, ref citizen, citizenData.m_targetBuilding, targeBuildingId);
                    return false;
                }
                else
                {
                    if (carRentalAI.m_rentedCarCount < carRentalAI.m_rentalCarCount)
                    {
                        TouristAIPatch.SpawnRentalVehicle(touristAI, instanceID, ref citizenData);
                        return false;
                    }
                    return true;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(HumanAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static void ArriveAtTarget(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData)
        {
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
            var visitBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizen.m_visitBuilding];
            var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            if (__instance.m_info.GetAI() is TouristAI && visitBuilding.Info.GetAI() is CarRentalAI && targetBuilding.Info.GetAI() is not CarRentalAI)
            {
                citizen.m_visitBuilding = citizenData.m_targetBuilding;
            }
        }
    }
}
