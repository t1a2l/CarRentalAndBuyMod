using CarRentalAndBuyMod.AI;
using ColossalFramework;
using HarmonyLib;
using System;

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


        [HarmonyPatch(typeof(VehicleAI), "Unspawn")]
        [HarmonyPostfix]
        public static void Unspawn(ushort vehicleID)
        {
            var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID];
            if(vehicle.m_sourceBuilding != 0 && vehicle.Info.GetAI() is PassengerCarAI)
            {
                ref var sourceBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[vehicle.m_sourceBuilding];
                if(sourceBuilding.Info.GetAI() is CarRentalAI carRentalAI)
                {
                    carRentalAI.m_rentedCarCount--;
                }
            }
        }
    }
}
