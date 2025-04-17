using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class ResidentAIPatch
    {
        [HarmonyPatch(typeof(ResidentAI), "SimulationStep", [typeof(ushort), typeof(CitizenInstance), typeof(CitizenInstance.Frame), typeof(bool)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void SimulationStep(ushort instanceID, ref CitizenInstance citizenData, ref CitizenInstance.Frame frameData, bool lodPhysics)
        {
            uint citizen = citizenData.m_citizen;
            if (citizen != 0)
            {
                bool shouldBuyVehicle = false;
                bool shouldSellVehicle = false;
                var vehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].m_vehicle;
                if(vehicleId == 0)
                {
                    shouldBuyVehicle = Singleton<SimulationManager>.instance.m_randomizer.Int32(32U) == 0;
                }
                else
                {
                    var vehicleInfo = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].Info;
                    if (vehicleInfo != null && vehicleInfo.GetAI() is not PassengerCarAI)
                    {
                        shouldBuyVehicle = Singleton<SimulationManager>.instance.m_randomizer.Int32(32U) == 0;
                    }
                    else
                    {
                        shouldSellVehicle = Singleton<SimulationManager>.instance.m_randomizer.Int32(32U) == 0;
                    }
                }

                if((shouldBuyVehicle || shouldSellVehicle) && HumanAIPatch.FindNearByCarShop(citizenData.m_frame0.m_position, "CarDealerAI"))
                {
                    var reason = ExtendedTransferManager.TransferReason.None;
                    if (shouldBuyVehicle)
                    {
                        reason = ExtendedTransferManager.TransferReason.CarBuy;
                    }
                    else if (shouldSellVehicle)
                    {
                        reason = ExtendedTransferManager.TransferReason.CarSell;
                    }
                    if (!CitizenDestinationManager.CitizenDestinationExist(citizenData.m_citizen) && citizenData.m_targetBuilding != 0)
                    {
                        var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
                        if(building.Info.GetAI() is not CarDealerAI)
                        {
                            CitizenDestinationManager.CreateCitizenDestination(citizenData.m_citizen, citizenData.m_targetBuilding);
                        }
                    }
                    ExtendedTransferManager.Offer offer = default;
                    offer.Citizen = citizenData.m_citizen;
                    offer.Position = citizenData.m_targetPos;
                    offer.Amount = 1;
                    offer.Active = true;
                    Singleton<ExtendedTransferManager>.instance.AddIncomingOffer(reason, offer);
                }
            }
        }

        [HarmonyPatch(typeof(ResidentAI), "SetTarget")]
        [HarmonyPrefix]
        public static void SetTarget(ResidentAI __instance, ushort instanceID, ref CitizenInstance data, ushort targetIndex, bool targetIsNode)
        {
            var vehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_vehicle;
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetIndex].Info.GetAI() is GasStationAI && vehicleId != 0 && VehicleFuelManager.VehicleFuelExist(vehicleId))
            {
                VehicleFuelManager.SetVehicleFuelOriginalTargetBuilding(vehicleId, data.m_targetBuilding);
            }
        }

        [HarmonyPatch(typeof(ResidentAI), "GetLocalizedStatus", [typeof(uint), typeof(Citizen), typeof(InstanceID)],
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
                        __result = "Going to sell owned car";
                    }
                    else
                    {
                        target = InstanceID.Empty;
                        __result = "Going to buy a new car";
                    }
                }
            }
        }

    }
}
