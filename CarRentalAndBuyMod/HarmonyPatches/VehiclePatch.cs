using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using System.Linq;

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
                var rentalBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[rentalObject.Value.CarRentalBuildingID];
                if(rentalBuilding.Info.GetAI() is CarRentalAI carRentalAI)
                {
                    carRentalAI.m_rentedCarCount--;
                }
                VehicleRentalManager.RemoveVehicleRental(rentalObject.Key);
            }

            if (VehicleFuelManager.VehiclesFuel.TryGetValue(vehicleID, out _))
            {
                VehicleFuelManager.RemoveVehicleFuel(vehicleID);
            }
        }

        [HarmonyPatch(typeof(Vehicle), "Spawn")]
        [HarmonyPostfix]
        public static void Spawn(ushort vehicleID)
        {
            if (!VehicleFuelManager.VehiclesFuel.TryGetValue(vehicleID, out _))
            {
                var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID];
                VehicleAI vehicleAI = (VehicleAI)vehicle.Info.GetAI();
                VehicleAIPatch.CreateFuelForVehicle(vehicleAI, vehicleID, ref vehicle);
            }
        }
    }
}
