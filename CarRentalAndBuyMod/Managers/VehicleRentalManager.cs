using System.Collections.Generic;

namespace CarRentalAndBuyMod.Managers
{
    public static class VehicleRentalManager
    {
        public static Dictionary<uint, Rental> VehicleRentals;

        public struct Rental
        {
            public ushort RentedVehicleID;
            public ushort CarRentalBuildingID;
        }

        public static void Init()
        {
            VehicleRentals ??= [];
        }

        public static void Deinit() => VehicleRentals = [];

        public static Rental GetVehicleRental(uint citizenId)
        {
            return VehicleRentals.TryGetValue(citizenId, out var rental) ? rental : default;
        }

        public static Rental CreateVehicleRental(uint citizenId, ushort rentedVehicleID, ushort carRentalBuildingID)
        {
            var rental = new Rental()
            {
                RentedVehicleID = rentedVehicleID,
                CarRentalBuildingID = carRentalBuildingID,
            };
            VehicleRentals.Add(citizenId, rental);

            return rental;
        }

        public static bool VehicleRentalExist(uint citizenId) => VehicleRentals.ContainsKey(citizenId);

        public static void SetVehicleRental(uint citizenId, Rental rental)
        {
            if (VehicleRentals.TryGetValue(citizenId, out var _))
            {
                VehicleRentals[citizenId] = rental;
            }
        }

        public static void RemoveVehicleRental(uint citizenId)
        {
            if (VehicleRentals.TryGetValue(citizenId, out var _))
            {
                VehicleRentals.Remove(citizenId);
            }
        }
    }

}
