using CarRentalAndBuyMod.Managers;
using HarmonyLib;
using System.Linq;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleManagerPatch
    {
        [HarmonyPatch(typeof(VehicleManager), "ReleaseParkedVehicle")]
        [HarmonyPrefix]
        public static void ReleaseParkedVehicle(ushort parked)
        {
            var rentalObject = VehicleRentalManager.VehicleRentals.Where(z => z.Value.RentedVehicleID == parked).FirstOrDefault();

            if (!rentalObject.Value.Equals(default(VehicleRentalManager.Rental)) && !rentalObject.Value.IsRemovedToSpawn)
            {
                VehicleRentalManager.RemoveVehicleRental(rentalObject.Key);
            }

            if (VehicleFuelManager.ParkedVehiclesFuel.TryGetValue(parked, out _))
            {
                VehicleFuelManager.RemoveParkedVehicleFuel(parked);
            }
        }
    }
}
