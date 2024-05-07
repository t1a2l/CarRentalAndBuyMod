﻿using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class WorldInfoPanelPatches
    {
        [HarmonyPatch(typeof(CitizenVehicleWorldInfoPanel), "UpdateBindings")]
        [HarmonyPostfix]
        public static void CitizenVehicleUpdateBindings(CitizenVehicleWorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
        {
            var Type = __instance.Find<UILabel>("Type");
            if (___m_InstanceID.Type == InstanceType.Vehicle && ___m_InstanceID.Vehicle != 0 && Type != null)
            {
                var info = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[___m_InstanceID.Vehicle].Info;
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(___m_InstanceID.Vehicle);
                if (!vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)))
                {
                    double value = (double)vehicleFuel.CurrentFuelCapacity / (double)vehicleFuel.MaxFuelCapacity;
                    bool isElectric = info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                    if(isElectric)
                    {
                        Type.text += "          Battery Percent:  " + value.ToString("#0%");
                    }
                    else
                    {
                        Type.text += "          Fuel Percent:  " + value.ToString("#0%");
                    }
                }
            }
            else if (___m_InstanceID.Type == InstanceType.ParkedVehicle && ___m_InstanceID.ParkedVehicle != 0 && Type != null)
            {
                var info = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[___m_InstanceID.ParkedVehicle].Info;
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(___m_InstanceID.ParkedVehicle);
                if (!vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)))
                {
                    double value = (double)vehicleFuel.CurrentFuelCapacity / (double)vehicleFuel.MaxFuelCapacity;
                    bool isElectric = info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                    if (isElectric)
                    {
                        Type.text += "          Battery Percent:  " + value.ToString("#0%");
                    }
                    else
                    {
                        Type.text += "          Fuel Percent:  " + value.ToString("#0%");
                    }
                }
            }

        }

        [HarmonyPatch(typeof(CityServiceVehicleWorldInfoPanel), "UpdateBindings")]
        [HarmonyPostfix]
        public static void CityServiceVehicleUpdateBindings(CityServiceVehicleWorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
        {
            var Type = __instance.Find<UILabel>("Type");
            if (___m_InstanceID.Vehicle != 0 && Type != null)
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(___m_InstanceID.Vehicle);
                if (!vehicleFuel.Equals(default(VehicleFuelManager.VehicleFuelCapacity)))
                {
                    double value = (double)vehicleFuel.CurrentFuelCapacity / (double)vehicleFuel.MaxFuelCapacity;
                    Type.text += "          Fuel Percent:  " + value.ToString("#0%");
                }    
            }
        }
    }
}