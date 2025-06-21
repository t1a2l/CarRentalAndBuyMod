using System.Runtime.CompilerServices;
using CarRentalAndBuyMod.Managers;
using HarmonyLib;
using MoreTransferReasons;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    public static class CarAIPatch
    {
        [HarmonyPatch(typeof(CarAI), "PathfindFailure")]
        [HarmonyPostfix]
        public static void PathfindFailure(ushort vehicleID, ref Vehicle data)
        {
            if ((data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle || data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle)
               && VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var fuelData = VehicleFuelManager.GetFuelData(vehicleID);
                var targetBuilding = fuelData.OriginalTargetBuilding;

                if (data.Info.m_vehicleAI is CargoTruckAI cargoTruckAI && (data.m_targetBuilding != 0))
                {
                    cargoTruckAI.SetTarget(vehicleID, ref data, targetBuilding);
                }
                else if (data.Info.m_vehicleAI is PassengerCarAI passengerCarAI && (data.m_targetBuilding != 0))
                {
                    passengerCarAI.SetTarget(vehicleID, ref data, targetBuilding);
                }
            }
        }

        [HarmonyPatch(typeof(CarAI), "StartPathFind",
           [typeof(ushort), typeof(Vehicle), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(bool), typeof(bool)],
           [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool BaseCarAIStartPathFind(CarAI instance, ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays, bool undergroundTarget)
        {
            return false;
        }
    }
}
