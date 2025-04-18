using System.Collections.Generic;

namespace CarRentalAndBuyMod.Managers
{
    public static class VehicleFuelManager
    {
        public static Dictionary<ushort, VehicleFuelCapacity> VehiclesFuel;

        public struct VehicleFuelCapacity
        {
            public float CurrentFuelCapacity;
            public float MaxFuelCapacity;
            public ushort OriginalTargetBuilding;
            public bool IsParked;
        }

        public static void Init()
        {
            VehiclesFuel ??= [];
        }

        public static void Deinit()
        {
            VehiclesFuel = [];
        }

        public static VehicleFuelCapacity GetFuelData(ushort vehicleId) => VehiclesFuel.TryGetValue(vehicleId, out var fuelCapacity) ? fuelCapacity : default;

        public static bool FuelDataExist(ushort vehicleId) => VehiclesFuel.ContainsKey(vehicleId);

        public static VehicleFuelCapacity CreateFuelData(ushort vehicleId, float currentFuelCapacity, float maxFuelCapacity, ushort originalTargetBuilding, bool isParked)
        {
            var vehicleFuelCapacity = new VehicleFuelCapacity
            {
                CurrentFuelCapacity = currentFuelCapacity,
                MaxFuelCapacity = maxFuelCapacity,
                OriginalTargetBuilding = originalTargetBuilding,
                IsParked = isParked
            };

            VehiclesFuel.Add(vehicleId, vehicleFuelCapacity);

            return vehicleFuelCapacity;
        }

        public static void SetOriginalTargetBuilding(ushort vehicleId, ushort originalTargetBuilding)
        {
            if (VehiclesFuel.TryGetValue(vehicleId, out var vehicleFuelCapacity))
            {
                vehicleFuelCapacity.OriginalTargetBuilding = originalTargetBuilding;
                VehiclesFuel[vehicleId] = vehicleFuelCapacity;
            }
        }

        public static void SetCurrentFuelCapacity(ushort vehicleId, float added_fuel)
        {
            if (VehiclesFuel.TryGetValue(vehicleId, out var vehicleFuelCapacity))
            {
                vehicleFuelCapacity.CurrentFuelCapacity += added_fuel;
                VehiclesFuel[vehicleId] = vehicleFuelCapacity;
            }
        }

        public static void UpdateParkingMode(ushort oldVehicleId, ushort newVehicleId, bool isParking)
        {
            if (VehiclesFuel.TryGetValue(oldVehicleId, out var vehicleFuelCapacity))
            {
                CreateFuelData(newVehicleId, vehicleFuelCapacity.CurrentFuelCapacity, vehicleFuelCapacity.MaxFuelCapacity, vehicleFuelCapacity.OriginalTargetBuilding, isParking);
                RemoveFuelData(oldVehicleId);
            }
        }

        public static void RemoveFuelData(ushort vehicleId)
        {
            if (VehiclesFuel.TryGetValue(vehicleId, out var _))
            {
                VehiclesFuel.Remove(vehicleId);
            }
        }

    }
}
