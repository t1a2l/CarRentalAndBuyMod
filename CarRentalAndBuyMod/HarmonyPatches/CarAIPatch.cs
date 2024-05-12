using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons.AI;

namespace CarRentalAndBuyMod.CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class CarAIPatch
    {
        [HarmonyPatch(typeof(CarAI), "SimulationStep",
            [ typeof(ushort), typeof(Vehicle), typeof(Vehicle.Frame), typeof(ushort), typeof(Vehicle), typeof(int)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void Prefix(CarAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
            if(vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)) && vehicleData.m_targetBuilding != 0)
            {
                CreateFuelForVehicle(__instance, vehicleID, ref vehicleData);
            }
        }

        private static void CreateFuelForVehicle(CarAI instance, ushort vehicleID, ref Vehicle data)
        {
            if (instance is ExtendedPassengerCarAI)
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(30, 60);
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 60, data.m_transferType, data.m_targetBuilding);
            }
            if (instance is ExtendedCargoTruckAI)
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(50, 80);
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 80, data.m_transferType, data.m_targetBuilding);
            }
        }
    }
}
