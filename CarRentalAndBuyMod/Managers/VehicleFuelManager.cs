using System.Collections.Generic;
using UnityEngine;
using static CarRentalAndBuyMod.Managers.VehicleFuelManager;

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
            public byte OriginalTransferReason;
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

        public static VehicleFuelCapacity GetVehicleFuel(ushort vehicleId) => !VehiclesFuel.TryGetValue(vehicleId, out var fuelCapacity) ? default : fuelCapacity;

        public static VehicleFuelCapacity GetParkedVehicleFuel(ushort vehicleId) => !ParkedVehiclesFuel.TryGetValue(vehicleId, out var fuelCapacity) ? default : fuelCapacity;

        public static void CreateVehicleFuel(ushort vehicleId, float currentFuelCapacity, float maxFuelCapacity, byte originalTransferReason, ushort originalTargetBuilding)
        {
            if (!VehiclesFuel.TryGetValue(vehicleId, out _))
            {
                var vehicleFuelCapacity = new VehicleFuelCapacity
                {
                    CurrentFuelCapacity = currentFuelCapacity,
                    MaxFuelCapacity = maxFuelCapacity,
                    OriginalTransferReason = originalTransferReason,
                    OriginalTargetBuilding = originalTargetBuilding
                };
                VehiclesFuel.Add(vehicleId, vehicleFuelCapacity);
            }
        }

        public static void CreateParkedVehicleFuel(ushort parkedVehicleId, float currentFuelCapacity, float maxFuelCapacity, byte originalTransferReason, ushort originalTargetBuilding)
        {
            if (!ParkedVehiclesFuel.TryGetValue(parkedVehicleId, out _))
            {
                var vehicleFuelCapacity = new VehicleFuelCapacity
                {
                    CurrentFuelCapacity = currentFuelCapacity,
                    MaxFuelCapacity = maxFuelCapacity,
                    OriginalTransferReason = originalTransferReason,
                    OriginalTargetBuilding = originalTargetBuilding
                };
                ParkedVehiclesFuel.Add(parkedVehicleId, vehicleFuelCapacity);
            }
        }

        public static void SetVehicleFuelOriginalTransferReason(ushort vehicleId, byte originalTransferReason)
        {
            if (VehiclesFuel.TryGetValue(vehicleId, out var vehicleFuelCapacity))
            {
                vehicleFuelCapacity.OriginalTransferReason = originalTransferReason;
                VehiclesFuel[vehicleId] = vehicleFuelCapacity;
            }
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
