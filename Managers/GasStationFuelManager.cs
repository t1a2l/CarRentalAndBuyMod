using System.Collections.Generic;

namespace CarRentalAndBuyMod.Managers
{
    public static class GasStationFuelManager
    {
        public static Dictionary<ushort, ushort> GasStationsFuel;

        public static void Init()
        {
            GasStationsFuel ??= [];
        }

        public static void Deinit() => GasStationsFuel = [];

        public static ushort GetGasStationFuel(ushort buildingId) => GasStationsFuel.TryGetValue(buildingId, out var fuelCapacity) ? fuelCapacity : default;

        public static ushort CreateGasStationFuel(ushort buildingId)
        {
            GasStationsFuel.Add(buildingId, 0);

            return 0;
        }

        public static bool GasStationFuelExist(ushort buildingId) => GasStationsFuel.ContainsKey(buildingId);

        public static void SetGasStationFuel(ushort buildingId, ushort fuelCapacity)
        {
            if (GasStationsFuel.TryGetValue(buildingId, out var _))
            {
                GasStationsFuel[buildingId] = fuelCapacity;
            }
        }

        public static void RemoveGasStationFuel(ushort buildingId)
        {
            if (GasStationsFuel.TryGetValue(buildingId, out var _))
            {
                GasStationsFuel.Remove(buildingId);
            }
        }
    }
}
