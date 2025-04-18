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
            public bool IsParked;
        }

        public static void Init()
        {
            VehicleRentals ??= [];
        }

        public static void Deinit() => VehicleRentals = [];

        public static Rental GetRentalData(uint citizenId)
        {
            return VehicleRentals.TryGetValue(citizenId, out var rental) ? rental : default;
        }

        public static bool RentalDataExist(uint citizenId) => VehicleRentals.ContainsKey(citizenId);

        public static Rental CreateRentalData(uint citizenId, ushort rentedVehicleID, ushort carRentalBuildingID)
        {
            var rental = new Rental()
            {
                RentedVehicleID = rentedVehicleID,
                CarRentalBuildingID = carRentalBuildingID,
            };
            VehicleRentals.Add(citizenId, rental);

            return rental;
        }

        public static void SetRentalData(uint citizenId, Rental rental)
        {
            if (VehicleRentals.TryGetValue(citizenId, out var _))
            {
                VehicleRentals[citizenId] = rental;
            }
        }

        public static void RemoveRentalData(uint citizenId)
        {
            if (VehicleRentals.TryGetValue(citizenId, out var _))
            {
                VehicleRentals.Remove(citizenId);
            }
        }
    }

}
