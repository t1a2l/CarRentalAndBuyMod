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
        public static void ParkVehiclePrefix(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, ref byte segmentOffset, ref uint __state)
        {
            CitizenManager instance2 = Singleton<CitizenManager>.instance;
            uint num = 0u;
            uint num2 = vehicleData.m_citizenUnits;
            int num3 = 0;
            while (num2 != 0 && num == 0)
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
        public static void ParkVehiclePostfix(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, ref byte segmentOffset, uint __state)
        {
            if(__state != 0)
            {
                CitizenManager instance2 = Singleton<CitizenManager>.instance;
                ushort parkedVehicle = instance2.m_citizens.m_buffer[__state].m_parkedVehicle;

                var rental = VehicleRentalManager.GetVehicleRental(__state);

                if(!rental.Equals(default(VehicleRentalManager.Rental)))
                {
                    Debug.Log("SetRentalParkingVehicle");
                    rental.RentedVehicleID = parkedVehicle;
                    VehicleRentalManager.SetVehicleRental(__state, rental);
                }
            }
        }


        [HarmonyPatch(typeof(PassengerCarAI), "GetColor", [typeof(ushort), typeof(Vehicle), typeof(InfoManager.InfoMode), typeof(InfoManager.SubInfoMode)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool GetColor(ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
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
        public static bool GetColor(ushort parkedVehicleID, ref VehicleParked data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
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

        [HarmonyPatch(typeof(PassengerCarAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static bool PassengerCarAIPrefix(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
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
            return true;
        }

        private static void FuelVehicle(ushort vehicleID, ref Vehicle data, GasStationAI gasStationAI, ref Building building)
        {
            data.m_flags |= Vehicle.Flags.Stopped;
            var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
            var neededFuel = vehicleFuel.MaxFuelCapacity - vehicleFuel.CurrentFuelCapacity;
            gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.FuelVehicle, ref neededFuel);
            VehicleFuelManager.SetVehicleFuel(vehicleID, neededFuel);
        }
    }
}
