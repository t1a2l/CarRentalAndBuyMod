using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class TouristAIPatch
    {
        public static ushort Chosen_Building = 0;

        [HarmonyPatch(typeof(TouristAI), "SimulationStep")]
        [HarmonyPrefix]
        public static void SimulationStep(uint citizenID, ref Citizen data)
        {
            if (citizenID != 0)
            {
                var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[data.m_instance];

                bool shouldRentVehicle = false;
                var vehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].m_vehicle;
                if (vehicleId == 0)
                {
                    shouldRentVehicle = Singleton<SimulationManager>.instance.m_randomizer.Int32(32U) == 0;
                }
                else
                {
                    var vehicleInfo = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].Info;
                    if (vehicleInfo != null && vehicleInfo.GetAI() is not PassengerCarAI)
                    {
                        shouldRentVehicle = Singleton<SimulationManager>.instance.m_randomizer.Int32(32U) == 0;
                    }
                }

                if(citizenInstance.m_targetBuilding != 0 && HumanAIPatch.IsRoadConnection(citizenInstance.m_targetBuilding))
                {
                    shouldRentVehicle = false;
                }

                if (shouldRentVehicle && HumanAIPatch.FindNearByCarShop(citizenInstance.m_frame0.m_position, "CarRentalAI"))
                {
                    if (!CitizenDestinationManager.CitizenDestinationExist(citizenID) && citizenInstance.m_targetBuilding != 0)
                    {
                        var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding];
                        if (building.Info.GetAI() is not CarDealerAI)
                        {
                            CitizenDestinationManager.CreateCitizenDestination(citizenID, citizenInstance.m_targetBuilding);
                        }
                    }
                    ExtendedTransferManager.Offer offer = default;
                    offer.Citizen = citizenID;
                    offer.Position = citizenInstance.m_targetPos;
                    offer.Amount = 1;
                    offer.Active = true;
                    Singleton<ExtendedTransferManager>.instance.AddIncomingOffer(ExtendedTransferManager.TransferReason.CarRent, offer);
                }
            }
        }

        [HarmonyPatch(typeof(TouristAI), "SetTarget")]
        [HarmonyPrefix]
        public static bool SetTarget(TouristAI __instance, ushort instanceID, ref CitizenInstance data, ushort targetIndex, bool targetIsNode)
        {
            var vehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_vehicle;
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetIndex].Info.GetAI() is GasStationAI && vehicleId != 0 && VehicleFuelManager.FuelDataExist(vehicleId))
            {
                VehicleFuelManager.SetOriginalTargetBuilding(vehicleId, data.m_targetBuilding);
            }
            if(VehicleRentalManager.RentalDataExist(data.m_citizen) && targetIndex != 0 && HumanAIPatch.IsRoadConnection(targetIndex) && !CitizenDestinationManager.CitizenDestinationExist(data.m_citizen))
            {
                Debug.Log("CarRentalAndBuyMod: TouristAI - SetTargetRoadConnection");
                CitizenDestinationManager.CreateCitizenDestination(data.m_citizen, targetIndex);
                var rental = VehicleRentalManager.GetRentalData(data.m_citizen);
                __instance.SetTarget(instanceID, ref data, rental.CarRentalBuildingID);
                return false;
            }
            return true;

        }

        [HarmonyPatch(typeof(TouristAI), "GetColor")]
        [HarmonyPrefix]
        public static bool GetColor(ushort instanceID, ref CitizenInstance data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            if (instanceID == 0)
            {
                return true;
            }

            if (infoMode == InfoManager.InfoMode.Tourism && subInfoMode == InfoManager.SubInfoMode.Attractiveness)
            {
                if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
                {
                    return true;
                }

                if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                {
                    Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
                }

                if (VehicleRentalManager.RentalDataExist(data.m_citizen))
                {
                    var rental = VehicleRentalManager.GetRentalData(data.m_citizen);
                    if (rental.CarRentalBuildingID == Chosen_Building)
                    {
                        __result = Color.yellow;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                }
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(TouristAI), "SpawnVehicle")]
        [HarmonyPrefix]
        public static bool SpawnVehicle(ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos, ref bool __result)
        {
            __result = HumanAIPatch.SpawnVehicle(instanceID, ref citizenData, pathPos);
            return false;
        }

        [HarmonyPatch(typeof(TouristAI), "GetLocalizedStatus", [typeof(uint), typeof(Citizen), typeof(InstanceID)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref])]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(uint citizenID, ref Citizen data, ref InstanceID target, ref string __result)
        {
            if (data.m_instance != 0)
            {
                var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[data.m_instance];
                var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding];
                if (targetBuilding.Info.GetAI() is CarDealerAI)
                {
                    var vehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].m_vehicle;
                    var parkedVehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].m_parkedVehicle;
                    if (vehicleId != 0 || parkedVehicleId != 0)
                    {
                        target = InstanceID.Empty;
                        __result = "Going to return rented car";
                    }
                    else
                    {
                        target = InstanceID.Empty;
                        __result = "Going to rent a car";
                    }
                }
            }
        }

    }
}