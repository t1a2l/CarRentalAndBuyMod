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

        [HarmonyPatch(typeof(VehicleAI), "CalculateTargetSpeed",
            [typeof(ushort), typeof(Vehicle), typeof(float), typeof(float)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void CalculateTargetSpeed(VehicleAI __instance, ushort vehicleID, ref Vehicle data, float speedLimit, float curve, ref float __result)
        {
            if (VehicleFuelManager.VehicleFuelExist(vehicleID) && (__instance is PassengerCarAI || __instance is ExtendedCargoTruckAI))
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                if (vehicleFuel.CurrentFuelCapacity < 10)
                {
                    __result = 0.5f;
                }
            }
        }

        [HarmonyPatch(typeof(VehicleAI), "SimulationStep",
            [typeof(ushort), typeof(Vehicle), typeof(Vehicle.Frame), typeof(ushort), typeof(Vehicle), typeof(int)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void SimulationStep(VehicleAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            if (VehicleFuelManager.VehicleFuelExist(vehicleID))
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                if (__instance is ExtendedCargoTruckAI || __instance is PassengerCarAI)
                {
                    bool isElectric = vehicleData.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                    if (vehicleData.m_custom != (ushort)ExtendedTransferManager.TransferReason.FuelVehicle && !isElectric)
                    {
                        float percent = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
                        VehicleFuelManager.SetVehicleFuelOriginalTargetBuilding(vehicleID, 0);
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
                                Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                            }
                            if (__instance is PassengerCarAI)
                            {
                                ExtendedTransferManager.Offer offer = default;
                                offer.Citizen = __instance.GetOwnerID(vehicleID, ref vehicleData).Citizen;
                                offer.Position = vehicleData.GetLastFramePosition();
                                offer.Amount = 1;
                                offer.Active = true;
                                Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                            }
                        }
                    }
                }
                if (vehicleFuel.CurrentFuelCapacity > 0)
                {
                    VehicleFuelManager.SetVehicleFuel(vehicleID, -0.01f);
                }
            }
        }

        public static void CreateFuelForVehicle(VehicleAI instance, ushort vehicleID, ref Vehicle data)
        {
            if (instance is PassengerCarAI && !VehicleFuelManager.VehicleFuelExist(vehicleID))
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(30, 60);
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 60, 0);
            }
            if (instance is ExtendedCargoTruckAI && !VehicleFuelManager.VehicleFuelExist(vehicleID))
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(50, 80);
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 80, 0);
            }
        }

    }
}
