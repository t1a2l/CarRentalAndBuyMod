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
        [HarmonyPatch(typeof(VehicleAI), "ReleaseVehicle")]
        [HarmonyPostfix]
        public static void ReleaseVehicle(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            VehicleFuelManager.RemoveVehicleFuel(vehicleID);
        }

        [HarmonyPatch(typeof(VehicleAI), "CalculateTargetSpeed",
            [typeof(ushort), typeof(Vehicle), typeof(float), typeof(float)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void CalculateTargetSpeed(VehicleAI __instance, ushort vehicleID, ref Vehicle data, float speedLimit, float curve, ref float __result)
        {
            if (__instance is ExtendedPassengerCarAI || __instance is ExtendedCargoTruckAI)
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                if (!vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)) && vehicleFuel.CurrentFuelCapacity < 10)
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
            if (__instance is ExtendedPassengerCarAI || __instance is ExtendedCargoTruckAI)
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                float percent = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
                bool shouldFuel = Singleton<SimulationManager>.instance.m_randomizer.Int32(32U) == 0;
                if ((percent > 0.2 && percent < 0.8 && shouldFuel) || percent <= 0.2)
                {
                    ExtendedTransferManager.Offer offer = default;
                    offer.Vehicle = vehicleID;
                    offer.Position = vehicleData.GetLastFramePosition();
                    offer.Amount = 1;
                    offer.Active = true;
                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                }
                if (vehicleFuel.CurrentFuelCapacity > 0)
                {
                    VehicleFuelManager.SetVehicleFuel(vehicleID, -0.01f);
                }
            }
        }

    }
}
