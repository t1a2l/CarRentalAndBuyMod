using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using HarmonyLib;
using System;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class PassengerCarAIPatch
    {
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
                        ushort instance5 = instance2.m_citizens.m_buffer[citizen].m_instance;
                        if (instance5 != 0)
                        {
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
                    rental.RentedVehicleID = parkedVehicle;
                    VehicleRentalManager.SetVehicleRental(__state, rental);
                }
            }
        }
    }
}
