﻿using CarRentalAndBuyMod.AI;
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
            if (data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle || data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle)
            {
                target = InstanceID.Empty;
                __result = "Getting fuel";
            }
        }

        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "SetTarget")]
        [HarmonyPrefix]
        public static void SetTarget(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].Info.GetAI() is GasStationAI && VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleID);
                if (vehicleFuel.OriginalTargetBuilding == 0 && data.m_targetBuilding != 0)
                {
                    VehicleFuelManager.SetOriginalTargetBuilding(vehicleID, data.m_targetBuilding);
                }
                if (!CustomCargoTruckAI.CustomStartPathFind(vehicleID, ref data))
                {
                    data.m_targetBuilding = 0;
                    __instance.SetTarget(vehicleID, ref data, 0);
                    data.Unspawn(vehicleID);
                }
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
            if ((data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle || data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle) 
                && VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleID);
                ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                if (building.Info.GetAI() is GasStationAI gasStationAI && distance < 80f)
                {
                    var neededFuel = (int)vehicleFuel.MaxFuelCapacity;
                    VehicleFuelManager.SetCurrentFuelCapacity(vehicleID, vehicleFuel.MaxFuelCapacity - vehicleFuel.CurrentFuelCapacity);
                    FuelVehicle(vehicleID, ref data, gasStationAI, ref building, neededFuel);
                    data.m_custom = 0;
                    var targetBuilding = vehicleFuel.OriginalTargetBuilding;
                    VehicleFuelManager.SetOriginalTargetBuilding(vehicleID, 0);
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
            bool isElectric = data.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
            if (!isElectric)
            {
                gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.FuelVehicle, ref neededFuel);
            }
            data.m_flags &= ~Vehicle.Flags.Stopped;
        }
    }
}
