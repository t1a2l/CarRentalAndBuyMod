using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons.AI;
using System.Linq;
using static RenderManager;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehiclePatch
    {
        [HarmonyPatch(typeof(Vehicle), "Unspawn")]
        [HarmonyPrefix]
        public static void Unspawn(ushort vehicleID)
        {
            var rentalObject = VehicleRentalManager.VehicleRentals.Where(z => z.Value.RentedVehicleID == vehicleID).FirstOrDefault();
            var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID];

            if (!rentalObject.Value.Equals(default(VehicleRentalManager.Rental)) && vehicle.Info.GetAI() is PassengerCarAI)
            {
                VehicleRentalManager.RemoveVehicleRental(rentalObject.Key);
            }
        }

        [HarmonyPatch(typeof(VehicleManager), "ReleaseParkedVehicle")]
        [HarmonyPrefix]
        public static void ReleaseParkedVehicle(ushort parked)
        {
            var rentalObject = VehicleRentalManager.VehicleRentals.Where(z => z.Value.RentedVehicleID == parked).FirstOrDefault();

            if (!rentalObject.Value.Equals(default(VehicleRentalManager.Rental)) && !rentalObject.Value.IsRemovedToSpawn)
            {
                VehicleRentalManager.RemoveVehicleRental(rentalObject.Key);
            }
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
                if (vehicleFuel.CurrentFuelCapacity < 10)
                {
                    __result = 0.5f;
                }
            }
        }

        [HarmonyPatch(typeof(VehicleAI), "LoadVehicle")]
        [HarmonyPostfix]
        public static void LoadVehicle(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
            if(vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)))
            {
                if (__instance is ExtendedPassengerCarAI)
                {
                    int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(30000, 60000);
                    VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 60000);
                }
                if (__instance is ExtendedCargoTruckAI)
                {
                    int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(50000, 80000);
                    VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 80000);
                }
            }
            else
            {
                if(vehicleFuel.MaxFuelCapacity == 60 || vehicleFuel.MaxFuelCapacity == 80)
                {
                    vehicleFuel.MaxFuelCapacity *= 1000;
                    vehicleFuel.CurrentFuelCapacity *= 1000;
                    VehicleFuelManager.VehiclesFuel[vehicleID] = vehicleFuel;
                }
            }
        }

    }
}
