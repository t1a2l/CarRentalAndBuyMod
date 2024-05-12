using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
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
            if (sourceBuilding.Info.GetAI() is CarRentalAI)
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
                            if (instance2 != 0 && (instance.m_instances.m_buffer[instance2].m_flags & CitizenInstance.Flags.EnteringVehicle) != 0 && citizen_source_building.Info.GetAI() is CarRentalAI)
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

        // get the tourist that parking his car
        [HarmonyBefore(["me.tmpe"])]
        [HarmonyPatch(typeof(PassengerCarAI), "ParkVehicle")]
        [HarmonyPrefix]
        public static void ParkVehiclePrefix(PassengerCarAI __instance, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, ref byte segmentOffset, ref uint __state)
        {
            CitizenManager instance2 = Singleton<CitizenManager>.instance;
            uint num2 = vehicleData.m_citizenUnits;
            int num3 = 0;
            while (num2 != 0)
            {
                uint nextUnit = instance2.m_units.m_buffer[num2].m_nextUnit;
                for (int i = 0; i < 5; i++)
                {
                    uint citizen = instance2.m_units.m_buffer[num2].GetCitizen(i);
                    if (citizen != 0)
                    {
                        var rental = VehicleRentalManager.GetVehicleRental(citizen);
                        ushort instance5 = instance2.m_citizens.m_buffer[citizen].m_instance;
                        if (instance5 != 0 && !rental.Equals(default(VehicleRentalManager.Rental)))
                        {
                            Debug.Log("GetRentalParkingVehicle");
                            __state = instance2.m_instances.m_buffer[instance5].m_citizen;
                            break;
                        }
                    }
                }
                num2 = nextUnit;
                if (++num3 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        // set the parked car as the toursit rental vehicle
        [HarmonyPatch(typeof(PassengerCarAI), "ParkVehicle")]
        [HarmonyPostfix]
        public static void ParkVehiclePostfix(PassengerCarAI __instance, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, ref byte segmentOffset, uint __state)
        {
            if(__state != 0)
            {
                CitizenManager instance2 = Singleton<CitizenManager>.instance;
                ushort parkedVehicle = instance2.m_citizens.m_buffer[__state].m_parkedVehicle;

                var rental = VehicleRentalManager.GetVehicleRental(__state);
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);

                if (!rental.Equals(default(VehicleRentalManager.Rental)))
                {
                    Debug.Log("SetRentalParkingVehicle");
                    rental.RentedVehicleID = parkedVehicle;
                    VehicleRentalManager.SetVehicleRental(__state, rental);
                }

                if (!vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)))
                {
                    VehicleFuelManager.CreateParkedVehicleFuel(parkedVehicle, vehicleFuel.CurrentFuelCapacity, vehicleFuel.MaxFuelCapacity, vehicleData.m_transferType, vehicleData.m_targetBuilding);
                    VehicleFuelManager.RemoveVehicleFuel(vehicleID);
                }
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

                var rental = VehicleRentalManager.VehicleRentals.Where(z => z.Value.RentedVehicleID == vehicleID).FirstOrDefault().Value;

                if (!rental.Equals(default(VehicleRentalManager.Rental)) && rental.CarRentalBuildingID == Chosen_Building)
                {
                    __result = Color.yellow;
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

                var rental = VehicleRentalManager.VehicleRentals.Where(z => z.Value.RentedVehicleID == parkedVehicleID).FirstOrDefault().Value;

                if (!rental.Equals(default(VehicleRentalManager.Rental)) && rental.CarRentalBuildingID == Chosen_Building)
                {
                    __result = Color.yellow;
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
            if(data.m_transferType >= 200)
            {
                byte transferType = (byte)(data.m_transferType - 200);
                if((ExtendedTransferManager.TransferReason)transferType == ExtendedTransferManager.TransferReason.FuelVehicle)
                {
                    target = InstanceID.Empty;
                    __result = "Getting fuel";
                }
            }
        }

        [HarmonyPatch(typeof(PassengerCarAI), "SetTarget")]
        [HarmonyPrefix]
        public static void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            if (data.m_transferType >= 200)
            {
                byte transferType = (byte)(data.m_transferType - 200);
                if ((ExtendedTransferManager.TransferReason)transferType == ExtendedTransferManager.TransferReason.FuelVehicle)
                {
                    ushort driverInstance = GetDriverInstance(vehicleID, ref data);
                    if (driverInstance != 0)
                    {
                        Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_targetBuilding = targetBuilding;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PassengerCarAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static bool PassengerCarAIPrefix(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
        {
            if (data.m_transferType >= 200)
            {
                byte transferType = (byte)(data.m_transferType - 200);
                if ((ExtendedTransferManager.TransferReason)transferType == ExtendedTransferManager.TransferReason.FuelVehicle)
                {
                    var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                    var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                    var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                    if (building.Info.GetAI() is GasStationAI gasStationAI && distance < 80f && !vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)))
                    {
                        var neededFuel = (int)vehicleFuel.MaxFuelCapacity;
                        VehicleFuelManager.SetVehicleFuel(vehicleID, vehicleFuel.MaxFuelCapacity - vehicleFuel.CurrentFuelCapacity);
                        FuelVehicle(vehicleID, ref data, gasStationAI, ref building, neededFuel);
                        data.m_transferType = vehicleFuel.OriginalTransferReason;
                        var targetBuilding = vehicleFuel.OriginalTargetBuilding;
                        ushort driverInstance = GetDriverInstance(vehicleID, ref data);
                        Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_targetBuilding = targetBuilding;
                        __instance.SetTarget(vehicleID, ref data, targetBuilding);
                        __result = false;
                        return false;
                    }
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
