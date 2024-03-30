using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class PassengerCarAIPatch
    {
        public static ushort Chosen_Building = 0;

        [HarmonyPatch(typeof(PassengerCarAI), "GetColor",
            [typeof(ushort), typeof(Vehicle), typeof(InfoManager.InfoMode), typeof(InfoManager.SubInfoMode)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool VehicleGetColor(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            if (vehicleID == 0)
            {
                return true;
            }

            if (infoMode == InfoManager.InfoMode.Traffic)
            {
                if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
                {
                    return true;
                }

                if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                {
                    Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
                }

                ushort source_building = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID].m_sourceBuilding;

                if (source_building == Chosen_Building)
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


        //[HarmonyPatch(typeof(PassengerCarAI), "GetColor",
        //    [typeof(ushort), typeof(VehicleParked), typeof(InfoManager.InfoMode), typeof(InfoManager.SubInfoMode)],
        //    [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        //[HarmonyPrefix]
        //public static bool VehicleParkedGetColor(PassengerCarAI __instance, ushort parkedVehicleID, ref VehicleParked data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        //{
        //    if (parkedVehicleID == 0)
        //    {
        //        return true;
        //    }

        //    if (infoMode == InfoManager.InfoMode.Connections)
        //    {
        //        if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
        //        {
        //            return true;
        //        }

        //        if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
        //        {
        //            Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
        //        }

        //        ushort source_building = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[parkedVehicleID].m_;

        //        if (source_building == Chosen_Building)
        //        {
        //            __result = Color.yellow;
        //        }
        //        else
        //        {
        //            __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
        //        }
        //        return false;
        //    }

        //    return true;
        //}
    }
}
