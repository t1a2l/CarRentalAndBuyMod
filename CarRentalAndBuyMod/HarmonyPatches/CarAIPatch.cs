using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using MoreTransferReasons.AI;
using CarRentalAndBuyMod.Managers;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch(typeof(CarAI))]
    public static class CarAIPatch
    {
        [HarmonyPatch(typeof(CarAI), "CreateVehicle")]
        [HarmonyPostfix]
        public static void CreateVehicle(CarAI __instance, ushort vehicleID, ref Vehicle data)
        {
            if(__instance is ExtendedPassengerCarAI)
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(30, 60);
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 60, data.m_transferType);
            }
            if (__instance is ExtendedCargoTruckAI)
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(50, 80);
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 80, data.m_transferType);
            }
        }

        [HarmonyPatch(typeof(CarAI), "ReleaseVehicle")]
        [HarmonyPostfix]
        public static void ReleaseVehicle(CarAI __instance, ushort vehicleID, ref Vehicle data)
        {
            if (__instance is ExtendedPassengerCarAI || __instance is ExtendedCargoTruckAI)
            {
                VehicleFuelManager.RemoveVehicleFuel(vehicleID);
            }
        }

        [HarmonyPatch(typeof(CarAI), "SimulationStep",
            [typeof(ushort), typeof(Vehicle), typeof(Vehicle.Frame), typeof(ushort), typeof(Vehicle), typeof(int)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void SimulationStep(CarAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            if (__instance is ExtendedPassengerCarAI || __instance is ExtendedCargoTruckAI)
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                float percent = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
                bool shouldFuel = Singleton<SimulationManager>.instance.m_randomizer.Int32(16U) == 0;
                if ((percent > 0.2 && shouldFuel) || percent <= 0.2)
                {
                    ExtendedTransferManager.Offer offer = default;
                    offer.Vehicle = vehicleID;
                    offer.Position = vehicleData.GetLastFramePosition();
                    offer.Amount = 1;
                    offer.Active = true;
                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                }
                if(vehicleFuel.CurrentFuelCapacity > 0)
                {
                    VehicleFuelManager.SetVehicleFuel(vehicleID, -0.01f);
                }
            }
        }
    }
}
