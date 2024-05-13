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
        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "GetLocalizedStatus")]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ref InstanceID target, ref string __result)
        {
            if (data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle)
            {
                target = InstanceID.Empty;
                __result = "Getting fuel";
            }
        }

        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static bool ArriveAtTarget(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
        {
            if (data.m_targetBuilding == 0)
            {
                return true;
            }
            if (data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle)
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                if (building.Info.GetAI() is GasStationAI gasStationAI && distance < 80f && !vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)))
                {
                    var neededFuel = (int)vehicleFuel.MaxFuelCapacity;
                    VehicleFuelManager.SetVehicleFuel(vehicleID, vehicleFuel.MaxFuelCapacity - vehicleFuel.CurrentFuelCapacity);
                    FuelVehicle(vehicleID, ref data, gasStationAI, ref building, neededFuel);
                    data.m_custom = 0;
                    var targetBuilding = vehicleFuel.OriginalTargetBuilding;
                    __instance.SetTarget(vehicleID, ref data, targetBuilding);
                    __result = false;
                    return false;
                }
            }
            return true;
        }

        private static void FuelVehicle(ushort vehicleID, ref Vehicle data, GasStationAI gasStationAI, ref Building building, int neededFuel)
        {
            data.m_flags |= Vehicle.Flags.Stopped;
            gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.FuelVehicle, ref neededFuel);
            data.m_flags &= ~Vehicle.Flags.Stopped;
        }
    }
}
