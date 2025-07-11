﻿using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using ColossalFramework.Globalization;
using HarmonyLib;
using MoreTransferReasons;
using System;
using System.Linq;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class PassengerCarAIPatch
    {
        public static ushort Chosen_Building = 0;

        [HarmonyPatch(typeof(PassengerCarAI), "CanLeave")]
        [HarmonyPrefix]
        public static bool CanLeave(PassengerCarAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref bool __result)
        {
            if(vehicleData.m_sourceBuilding == 0)
            {
                return true;
            }
            var sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding];
            if (sourceBuilding.Info.GetAI() is CarRentalAI || sourceBuilding.Info.GetAI() is CarDealerAI)
            {
                CitizenManager instance = Singleton<CitizenManager>.instance;
                uint num = vehicleData.m_citizenUnits;
                int num2 = 0;
                while (num != 0)
                {
                    uint nextUnit = instance.m_units.m_buffer[num].m_nextUnit;
                    for (int i = 0; i < 5; i++)
                    {
                        uint citizenId = instance.m_units.m_buffer[num].GetCitizen(i);
                        if (citizenId != 0)
                        {
                            ushort instance2 = instance.m_citizens.m_buffer[citizenId].m_instance;
                            var citizen_source_building_id = instance.m_instances.m_buffer[instance2].m_sourceBuilding;
                            var citizen_source_building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizen_source_building_id];
                            if (instance2 != 0 && (instance.m_instances.m_buffer[instance2].m_flags & CitizenInstance.Flags.EnteringVehicle) != 0 
                                && (citizen_source_building.Info.GetAI() is CarRentalAI || citizen_source_building.Info.GetAI() is CarDealerAI))
                            {
                                __result = false;
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    num = nextUnit;
                    if (++num2 > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PassengerCarAI), "ParkVehicle")]
        [HarmonyPostfix]
        public static void ParkVehiclePostfix(PassengerCarAI __instance, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, ref byte segmentOffset)
        {
            var citizenId = __instance.GetOwnerID(vehicleID, ref vehicleData).Citizen;
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

            if (citizen.m_parkedVehicle != 0 && VehicleRentalManager.RentalDataExist(citizenId))
            {
                Debug.Log("CarRentalAndBuyMod: PassengerCarAI - SetRentalParkingVehicle");
                var rental = VehicleRentalManager.GetRentalData(citizenId);
                rental.RentedVehicleID = citizen.m_parkedVehicle;
                rental.IsParked = true;
                VehicleRentalManager.SetRentalData(citizenId, rental);
            }

            if (citizen.m_parkedVehicle != 0 && VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var fuelData = VehicleFuelManager.GetFuelData(vehicleID);
                if (vehicleData.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle || 
                    vehicleData.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle)
                {
                    var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                    ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding];
                    if (building.Info.GetAI() is GasStationAI gasStationAI)
                    {
                        VehicleFuelManager.SetCurrentFuelCapacity(vehicleID, fuelData.MaxFuelCapacity - fuelData.CurrentFuelCapacity);
                        var neededFuel = (int)fuelData.MaxFuelCapacity;
                        FuelVehicle(vehicleID, ref vehicleData, gasStationAI, ref building, neededFuel);
                        vehicleData.m_custom = 0;
                        fuelData = VehicleFuelManager.GetFuelData(vehicleID);
                    }
                }
                VehicleFuelManager.CreateParkedFuelData(citizen.m_parkedVehicle, fuelData.CurrentFuelCapacity, fuelData.MaxFuelCapacity, fuelData.OriginalTargetBuilding);
                VehicleFuelManager.RemoveFuelData(vehicleID);
            }
        }

        [HarmonyPatch(typeof(PassengerCarAI), "GetColor", [typeof(ushort), typeof(Vehicle), typeof(InfoManager.InfoMode), typeof(InfoManager.SubInfoMode)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool GetColor(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            if (vehicleID == 0)
            {
                return true;
            }

            if (infoMode == InfoManager.InfoMode.Tourism && subInfoMode == InfoManager.SubInfoMode.Attractiveness)
            {
                if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
                {
                    return true;
                }

                if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                {
                    Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
                }

                var rentalKey = VehicleRentalManager.VehicleRentals.FirstOrDefault(kvp => kvp.Value.RentedVehicleID == vehicleID).Key;

                if (rentalKey != 0)
                {
                    var rental = VehicleRentalManager.GetRentalData(rentalKey);
                    if (rental.CarRentalBuildingID == Chosen_Building)
                    {
                        __result = Color.yellow;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                }
                else
                {
                    __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                }

                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PassengerCarAI), "GetColor", [typeof(ushort), typeof(VehicleParked), typeof(InfoManager.InfoMode), typeof(InfoManager.SubInfoMode)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool GetColor(PassengerCarAI __instance, ushort parkedVehicleID, ref VehicleParked data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            if (parkedVehicleID == 0)
            {
                return true;
            }

            if (infoMode == InfoManager.InfoMode.Tourism && subInfoMode == InfoManager.SubInfoMode.Attractiveness)
            {
                if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
                {
                    return true;
                }

                if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                {
                    Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
                }

                var rentalKey = VehicleRentalManager.VehicleRentals.FirstOrDefault(kvp => kvp.Value.RentedVehicleID == parkedVehicleID).Key;

                if (rentalKey != 0)
                {
                    var rental = VehicleRentalManager.GetRentalData(rentalKey);
                    if (rental.CarRentalBuildingID == Chosen_Building)
                    {
                        __result = Color.yellow;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                }
                else
                {
                    __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                }

                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PassengerCarAI), "GetLocalizedStatus", [typeof(ushort), typeof(Vehicle), typeof(InstanceID)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref])]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, ref InstanceID target, ref string __result)
        {
            var citizenId = __instance.GetOwnerID(vehicleID, ref data).Citizen;
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
            var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
            if ((data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle || data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle) 
                && VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleID);
                target = InstanceID.Empty;
                __result = "Getting fuel";
                if (citizenInstance.m_targetBuilding == vehicleFuel.OriginalTargetBuilding)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result += " and " + Locale.Get("VEHICLE_STATUS_GOINGTO");
                }
            }
        }

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(PassengerCarAI), "SetTarget")]
        [HarmonyPrefix]
        public static bool SetTarget(ref CargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            var citizenId = GetOwnerID(vehicleID, ref data).Citizen;
            if(citizenId != 0)
            {
                var citizenInstanceId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId].m_instance;
                if(citizenInstanceId != 0)
                {
                    ref var citizenInstance = ref Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId];
                    if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding].Info.GetAI() is GasStationAI 
                        && VehicleFuelManager.FuelDataExist(vehicleID) && data.m_custom != 0)
                    {
                        data.m_targetBuilding = citizenInstance.m_targetBuilding;
                        var pathToGasStation = CustomCargoTruckAI.CustomStartPathFind(vehicleID, ref data);
                        var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleID);
                        if (!pathToGasStation)
                        {
                            data.m_targetBuilding = 0;
                            data.m_custom = 0;
                            citizenInstance.m_targetBuilding = vehicleFuel.OriginalTargetBuilding;
                            __instance.SetTarget(vehicleID, ref data, vehicleFuel.OriginalTargetBuilding);
                            data.Unspawn(vehicleID);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PassengerCarAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static bool ArriveAtTarget(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
        {
            if ((data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle || data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle) 
                && VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleID);
                var neededFuel = (int)vehicleFuel.MaxFuelCapacity;
                var citizenId = __instance.GetOwnerID(vehicleID, ref data).Citizen;
                ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding];
                var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                if (building.Info.GetAI() is GasStationAI gasStationAI && distance < 80f)
                {
                    VehicleFuelManager.SetCurrentFuelCapacity(vehicleID, vehicleFuel.MaxFuelCapacity - vehicleFuel.CurrentFuelCapacity);
                    FuelVehicle(vehicleID, ref data, gasStationAI, ref building, neededFuel);
                    data.m_custom = 0;
                    var originalTargetBuilding = vehicleFuel.OriginalTargetBuilding;
                    var humanAI = citizen.GetCitizenInfo(citizenId).GetAI() as HumanAI;
                    humanAI.StartMoving(citizenId, ref citizen, citizenInstance.m_targetBuilding, originalTargetBuilding);
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
            if(!isElectric)
            {
                gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.FuelVehicle, ref neededFuel);
            }
            data.m_flags &= ~Vehicle.Flags.Stopped;
        }


        private static InstanceID GetOwnerID(ushort vehicleID, ref Vehicle vehicleData)
        {
            InstanceID result = default;
            ushort driverInstance = GetDriverInstance(vehicleID, ref vehicleData);
            if (driverInstance != 0)
            {
                result.Citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_citizen;
            }
            return result;
        }

        private static ushort GetDriverInstance(ushort vehicleID, ref Vehicle data)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = data.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                uint nextUnit = instance.m_units.m_buffer[num].m_nextUnit;
                for (int i = 0; i < 5; i++)
                {
                    uint citizen = instance.m_units.m_buffer[num].GetCitizen(i);
                    if (citizen != 0)
                    {
                        ushort instance2 = instance.m_citizens.m_buffer[citizen].m_instance;
                        if (instance2 != 0)
                        {
                            return instance2;
                        }
                    }
                }
                num = nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return 0;
        }
    }
}
