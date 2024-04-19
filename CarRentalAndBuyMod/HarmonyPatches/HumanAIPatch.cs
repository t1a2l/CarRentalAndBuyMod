﻿using CarRentalAndBuyMod.AI;
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
                var rental = VehicleRentalManager.GetVehicleRental(citizenData.m_citizen);
                // i am here to return the car and leave the city
                if (!rental.Equals(default(VehicleRentalManager.Rental)) && rental.CarRentalBuildingID == citizenData.m_targetBuilding)
                {
                    // get original outside connection target
                    var targeBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                    CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);
                    Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, 0, 0u);
                    VehicleRentalManager.RemoveVehicleRental(citizenData.m_citizen);
                    vehicle.Unspawn(citizen.m_vehicle);
                    // move to outside connection
                    __instance.SetTarget(instanceID, ref citizenData, targeBuildingId);
                    return false;
                }
                else
                {
                    if (carRentalAI.m_rentedCarCount < carRentalAI.m_rentalCarCount)
                    {
                        VehicleInfo vehicleInfo = TouristAIPatch.GetRentalVehicleInfo(ref citizenData);
                        var targeBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                        if (targeBuildingId != 0)
                        {
                            CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);
                            __instance.SetTarget(instanceID, ref citizenData, targeBuildingId, false);
                        }
                        Singleton<PathManager>.instance.m_pathUnits.m_buffer[citizenData.m_path].GetPosition(citizenData.m_pathPositionIndex >> 1, out var position);
                        TouristAIPatch.SpawnRentalVehicle(touristAI, instanceID, ref citizenData, vehicleInfo, position);
                        VehicleRentalManager.CreateVehicleRental(citizenData.m_citizen, citizen.m_vehicle, citizenData.m_targetBuilding);
                        carRentalAI.m_rentedCarCount++;
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
