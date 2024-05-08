using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons.AI;
using MoreTransferReasons;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class ExtendedCargoTruckAIPatch
    {
        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static bool ArriveAtTarget(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
        {
            if (data.m_targetBuilding == 0)
            {
                return true;
            }
            if (data.m_transferType >= 200)
            {
                byte transferType = (byte)(data.m_transferType - 200);
                if ((ExtendedTransferManager.TransferReason)transferType == ExtendedTransferManager.TransferReason.FuelVehicle)
                {
                    var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                    var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                    if (building.Info.GetAI() is GasStationAI gasStationAI && distance < 80f)
                    {
                        FuelVehicle(vehicleID, ref data, gasStationAI, ref building);
                        __instance.SetTarget(vehicleID, ref data, 0);
                        __result = true;
                        return false;
                    }
                }
            }
            return true;
        }

        private static void FuelVehicle(ushort vehicleID, ref Vehicle data, GasStationAI gasStationAI, ref Building building)
        {
            data.m_flags |= Vehicle.Flags.Stopped;
            var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
            var neededFuel = (int)vehicleFuel.MaxFuelCapacity;
            gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.FuelVehicle, ref neededFuel);
            VehicleFuelManager.SetVehicleFuel(vehicleID, vehicleFuel.MaxFuelCapacity - vehicleFuel.CurrentFuelCapacity);
        }
    }
}
