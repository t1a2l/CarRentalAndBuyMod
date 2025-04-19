using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using MoreTransferReasons.AI;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleAIPatch
    {
        [HarmonyPatch(typeof(VehicleAI), "CreateVehicle")]
        [HarmonyPostfix]
        public static void CreateVehicle(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            CreateFuelForVehicle(__instance, vehicleID, ref data);
        }

        [HarmonyPatch(typeof(VehicleAI), "LoadVehicle")]
        [HarmonyPostfix]
        public static void LoadVehicle(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            CreateFuelForVehicle(__instance, vehicleID, ref data);
        }

        [HarmonyPatch(typeof(VehicleAI), "SimulationStep",
            [typeof(ushort), typeof(Vehicle), typeof(Vehicle.Frame), typeof(ushort), typeof(Vehicle), typeof(int)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void SimulationStep(VehicleAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            if (VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleID);
                if (__instance is ExtendedCargoTruckAI || __instance is PassengerCarAI)
                {
                    if (vehicleData.m_custom != (ushort)ExtendedTransferManager.TransferReason.FuelVehicle && 
                        vehicleData.m_custom != (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle)
                    {
                        float percent = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
                        VehicleFuelManager.SetOriginalTargetBuilding(vehicleID, 0);
                        bool shouldFuel = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;
                        if ((percent > 0.2 && percent < 0.8 && shouldFuel) || percent <= 0.2)
                        {
                            if (__instance is ExtendedCargoTruckAI)
                            {
                                ExtendedTransferManager.Offer offer = default;
                                offer.Vehicle = vehicleID;
                                offer.Position = vehicleData.GetLastFramePosition();
                                offer.Amount = 1;
                                offer.Active = true;
                                bool isElectric = vehicleData.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                                if (isElectric)
                                {
                                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelElectricVehicle, offer);
                                }
                                else
                                {
                                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                                }
                            }
                            if (__instance is PassengerCarAI)
                            {
                                ExtendedTransferManager.Offer offer = default;
                                offer.Citizen = __instance.GetOwnerID(vehicleID, ref vehicleData).Citizen;
                                offer.Position = vehicleData.GetLastFramePosition();
                                offer.Amount = 1;
                                offer.Active = true;
                                bool isElectric = vehicleData.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                                if(isElectric)
                                {
                                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelElectricVehicle, offer);
                                }
                                else
                                {
                                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                                }   
                            }
                        }
                    }
                }
                if (vehicleFuel.CurrentFuelCapacity > 0)
                {
                    VehicleFuelManager.SetCurrentFuelCapacity(vehicleID, -0.01f);
                }
            }
        }

        private static void CreateFuelForVehicle(VehicleAI instance, ushort vehicleID, ref Vehicle data)
        {
            if (instance is PassengerCarAI && !VehicleFuelManager.FuelDataExist(vehicleID))
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(30, 60);
                VehicleFuelManager.CreateFuelData(vehicleID, randomFuelCapacity, 60, 0);
            }
            if (instance is ExtendedCargoTruckAI && !VehicleFuelManager.FuelDataExist(vehicleID))
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(50, 80);
                VehicleFuelManager.CreateFuelData(vehicleID, randomFuelCapacity, 80, 0);
            }
        }

    }
}
