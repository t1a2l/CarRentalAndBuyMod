using System.Collections.Generic;

namespace CarRentalAndBuyMod.Utils
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
            return !VehicleRentals.TryGetValue(citizenId, out var rental) ? default : rental;
        }

        public static void CreateVehicleRental(uint citizenId, ushort rentedVehicleID, ushort carRentalBuildingID)
        {
            if (!VehicleRentals.TryGetValue(citizenId, out _))
            {
                var rental = new Rental()
                {
                    RentedVehicleID = rentedVehicleID,
                    CarRentalBuildingID = carRentalBuildingID
                };
                VehicleRentals.Add(citizenId, rental);
            }
        }

        public static void SetVehicleRental(uint citizenId, Rental rental) => VehicleRentals[citizenId] = rental;


        public static void RemoveVehicleRental(uint citizenId) => VehicleRentals.Remove(citizenId);
    }

}
