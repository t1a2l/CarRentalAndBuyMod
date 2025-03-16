using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using UnityEngine;

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
            ref var targetBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle];

            if (__instance.m_info.GetAI() is TouristAI touristAI && targetBuilding.Info.GetAI() is CarRentalAI carRentalAI)
            {
                if(VehicleRentalManager.VehicleRentalExist(citizenData.m_citizen))
                {
                    var rental = VehicleRentalManager.GetVehicleRental(citizenData.m_citizen);
                    // i am here to return the car and leave the city
                    if (rental.CarRentalBuildingID == citizenData.m_targetBuilding && CitizenDestinationManager.CitizenDestinationExist(citizenData.m_citizen))
                    {
                        Debug.Log("CarRentalAndBuyMod: HumanAI - ReturnRentalVehicle");
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
                }
                else
                {
                    if (carRentalAI.m_rentedCarCount < targetBuilding.m_customBuffer1)
                    {
                        Debug.Log("CarRentalAndBuyMod: HumanAI - RentNewRentalVehicle");
                        VehicleInfo vehicleInfo = VehicleManagerPatch.GetVehicleInfo(ref citizenData); 
                        TouristAIPatch.SpawnRentalVehicle(touristAI, instanceID, ref citizenData, vehicleInfo, default);
                        VehicleRentalManager.CreateVehicleRental(citizenData.m_citizen, citizen.m_vehicle, citizenData.m_sourceBuilding);
                        carRentalAI.m_rentedCarCount++;
                        return false;
                    }
                    return true;
                }
            }
            else if (__instance.m_info.GetAI() is ResidentAI residentAI && targetBuilding.Info.GetAI() is CarDealerAI)
            {
                if (targetBuilding.m_customBuffer1 > 0)
                {
                    Debug.Log("CarRentalAndBuyMod: HumanAI - BuyNewVehicle");
                    VehicleInfo vehicleInfo = VehicleManagerPatch.GetVehicleInfo(ref citizenData);
                    ResidentAIPatch.SpawnOwnVehicle(residentAI, instanceID, ref citizenData, vehicleInfo, default);
                    targetBuilding.m_customBuffer1--;
                    return false;
                }
                return true;
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
                Debug.Log("CarRentalAndBuyMod: HumanAI - ArrivingAtTargetTouristAI");
                citizen.m_visitBuilding = citizenData.m_targetBuilding;
            }
            else if (__instance.m_info.GetAI() is ResidentAI && visitBuilding.Info.GetAI() is CarDealerAI && targetBuilding.Info.GetAI() is not CarDealerAI)
            {
                Debug.Log("CarRentalAndBuyMod: HumanAI - ArrivingAtTargetResidentAI");
                citizen.m_visitBuilding = citizenData.m_targetBuilding;
            }
        }

        [HarmonyPatch(typeof(HumanAI), "SimulationStep", [typeof(ushort), typeof(CitizenInstance), typeof(CitizenInstance.Frame), typeof(bool)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool SimulationStep(ushort instanceID, ref CitizenInstance citizenData, ref CitizenInstance.Frame frameData, bool lodPhysics)
        {
            if ((citizenData.m_flags & CitizenInstance.Flags.Blown) == 0 && (citizenData.m_flags & CitizenInstance.Flags.Floating) == 0
                && (citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) == 0 && (citizenData.m_flags & CitizenInstance.Flags.WaitingTransport) == 0
                && (citizenData.m_flags & CitizenInstance.Flags.WaitingTaxi) == 0 && (citizenData.m_flags & CitizenInstance.Flags.EnteringVehicle) == 0)
            {
                ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
                var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle];
                if (citizen.m_vehicle != 0 && vehicle.Info.GetAI() is PassengerCarAI) 
                {
                    var vehicleFuel = VehicleFuelManager.GetVehicleFuel(citizen.m_vehicle);
                    bool isElectric = vehicle.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                    if (vehicle.m_custom != (ushort)ExtendedTransferManager.TransferReason.FuelVehicle && !isElectric)
                    {
                        float percent = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
                        VehicleFuelManager.SetVehicleFuelOriginalTargetBuilding(citizen.m_vehicle, 0);
                        bool shouldFuel = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;
                        if ((percent > 0.2 && percent < 0.8 && shouldFuel) || percent <= 0.2)
                        {
                            ExtendedTransferManager.Offer offer = default;
                            offer.Citizen = citizenData.m_citizen;
                            offer.Position = frameData.m_position;
                            offer.Amount = 1;
                            offer.Active = true;
                            Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                        }
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
