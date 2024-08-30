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
            public ushort OriginalTargetBuilding;
        }

        public static void Init()
        {
            VehiclesFuel ??= [];
            ParkedVehiclesFuel ??= [];
        }

        public static void Deinit()
        {
            VehiclesFuel = [];
            ParkedVehiclesFuel = [];
        }

        public static VehicleFuelCapacity GetVehicleFuel(ushort vehicleId) => VehiclesFuel.TryGetValue(vehicleId, out var fuelCapacity) ? fuelCapacity : default;

        public static VehicleFuelCapacity GetParkedVehicleFuel(ushort vehicleId) => ParkedVehiclesFuel.TryGetValue(vehicleId, out var fuelCapacity) ? fuelCapacity : default;

        public static bool VehicleFuelExist(ushort vehicleId) => VehiclesFuel.ContainsKey(vehicleId);

        public static bool ParkedVehicleFuelExist(ushort parkedVehicleId) => ParkedVehiclesFuel.ContainsKey(parkedVehicleId);

        public static VehicleFuelCapacity CreateVehicleFuel(ushort vehicleId, float currentFuelCapacity, float maxFuelCapacity, ushort originalTargetBuilding)
        {
            var vehicleFuelCapacity = new VehicleFuelCapacity
            {
                CurrentFuelCapacity = currentFuelCapacity,
                MaxFuelCapacity = maxFuelCapacity,
                OriginalTargetBuilding = originalTargetBuilding
            };

            VehiclesFuel.Add(vehicleId, vehicleFuelCapacity);

            return vehicleFuelCapacity;
        }

        public static VehicleFuelCapacity CreateParkedVehicleFuel(ushort parkedVehicleId, float currentFuelCapacity, float maxFuelCapacity, ushort originalTargetBuilding)
        {
            var vehicleFuelCapacity = new VehicleFuelCapacity
            {
                CurrentFuelCapacity = currentFuelCapacity,
                MaxFuelCapacity = maxFuelCapacity,
                OriginalTargetBuilding = originalTargetBuilding
            };

            ParkedVehiclesFuel.Add(parkedVehicleId, vehicleFuelCapacity);

            return vehicleFuelCapacity;
        }


        public static void SetVehicleFuelOriginalTargetBuilding(ushort vehicleId, ushort originalTargetBuilding)
        {
            if (VehiclesFuel.TryGetValue(vehicleId, out var vehicleFuelCapacity))
            {
                vehicleFuelCapacity.OriginalTargetBuilding = originalTargetBuilding;
                VehiclesFuel[vehicleId] = vehicleFuelCapacity;
            }
        }

        public static void SetVehicleFuel(ushort vehicleId, float added_fuel)
        {
            if (VehiclesFuel.TryGetValue(vehicleId, out var vehicleFuelCapacity))
            {
                vehicleFuelCapacity.CurrentFuelCapacity += added_fuel;
                VehiclesFuel[vehicleId] = vehicleFuelCapacity;
            }
        }

        public static void SetParkedVehicleFuel(ushort parkedVehicleId, float added_fuel)
        {
            if (ParkedVehiclesFuel.TryGetValue(parkedVehicleId, out var vehicleFuelCapacity))
            {
                vehicleFuelCapacity.CurrentFuelCapacity += added_fuel;
                ParkedVehiclesFuel[parkedVehicleId] = vehicleFuelCapacity;
            }
        }

        public static void RemoveVehicleFuel(ushort vehicleId)
        {
            if (VehiclesFuel.TryGetValue(vehicleId, out var _))
            {
                VehiclesFuel.Remove(vehicleId);
            }
        }

        public static void RemoveParkedVehicleFuel(ushort parkedVehicleId)
        {
            if (ParkedVehiclesFuel.TryGetValue(parkedVehicleId, out var _))
            {
                ParkedVehiclesFuel.Remove(parkedVehicleId);
            }
        }
    }
}
