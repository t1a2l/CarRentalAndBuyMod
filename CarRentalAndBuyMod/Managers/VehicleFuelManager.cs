using System.Collections.Generic;

namespace CarRentalAndBuyMod.Managers
{
    public static class VehicleFuelManager
    {
        public static Dictionary<ushort, VehicleFuelCapacity> VehiclesFuel;

        public static Dictionary<ushort, VehicleFuelCapacity> ParkedVehiclesFuel;

        public struct VehicleFuelCapacity
        {
            public float CurrentFuelCapacity;
            public float MaxFuelCapacity;
        }

        public static void Init()
        {
            VehiclesFuel ??= [];
        }

        public static void Deinit() => VehiclesFuel = [];

        public static VehicleFuelCapacity GetVehicleFuel(ushort vehicleId) => !VehiclesFuel.TryGetValue(vehicleId, out var fuelCapacity) ? default : fuelCapacity;

        public static VehicleFuelCapacity GetParkedVehicleFuel(ushort vehicleId) => !ParkedVehiclesFuel.TryGetValue(vehicleId, out var fuelCapacity) ? default : fuelCapacity;

        public static void CreateVehicleFuel(ushort vehicleId, float currentFuelCapacity, float maxFuelCapacity)
        {
            if (!VehiclesFuel.TryGetValue(vehicleId, out _))
            {
                var vehicleFuelCapacity = new VehicleFuelCapacity
                {
                    CurrentFuelCapacity = currentFuelCapacity,
                    MaxFuelCapacity = maxFuelCapacity
                };
                VehiclesFuel.Add(vehicleId, vehicleFuelCapacity);
            }
        }

        public static void CreateParkedVehicleFuel(ushort parkedVehicleId, float currentFuelCapacity, float maxFuelCapacity)
        {
            if (!ParkedVehiclesFuel.TryGetValue(parkedVehicleId, out _))
            {
                var vehicleFuelCapacity = new VehicleFuelCapacity
                {
                    CurrentFuelCapacity = currentFuelCapacity,
                    MaxFuelCapacity = maxFuelCapacity
                };
                ParkedVehiclesFuel.Add(parkedVehicleId, vehicleFuelCapacity);
            }
        }

        public static void SetVehicleFuel(ushort vehicleId, float added_fuel)
        {
            var vehicleFuelCapacity = VehiclesFuel[vehicleId];
            vehicleFuelCapacity.CurrentFuelCapacity += added_fuel;
            VehiclesFuel[vehicleId] = vehicleFuelCapacity;
        }

        public static void SetParkedVehicleFuel(ushort parkedVehicleId, float added_fuel)
        {
            var vehicleFuelCapacity = ParkedVehiclesFuel[parkedVehicleId];
            vehicleFuelCapacity.CurrentFuelCapacity += added_fuel;
            ParkedVehiclesFuel[parkedVehicleId] = vehicleFuelCapacity;
        }

        public static void RemoveVehicleFuel(ushort vehicleId) => VehiclesFuel.Remove(vehicleId);

        public static void RemoveParkedVehicleFuel(ushort parkedVehicleId) => ParkedVehiclesFuel.Remove(parkedVehicleId);
    }
}
