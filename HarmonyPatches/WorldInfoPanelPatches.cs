using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using System;

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
            var panel = __instance.Find<UIPanel>("(Library) CitizenVehicleWorldInfoPanel");
            if(panel != null)
            {
                panel.height = 290;
            }
            if(Type == null)
            {
                return;
            }
            ushort vehicleId = 0;
            VehicleInfo vehicleInfo = null;
            double value = 0;
            if (___m_InstanceID.Type == InstanceType.Vehicle && ___m_InstanceID.Vehicle != 0 && VehicleFuelManager.FuelDataExist(___m_InstanceID.Vehicle))
            {
                vehicleId = ___m_InstanceID.Vehicle;
                vehicleInfo = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[___m_InstanceID.Vehicle].Info;
                var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleId);
                value = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
            }
            else if(___m_InstanceID.Type == InstanceType.ParkedVehicle && ___m_InstanceID.ParkedVehicle != 0 && VehicleFuelManager.ParkedFuelDataExist(___m_InstanceID.ParkedVehicle))
            {
                vehicleId = ___m_InstanceID.ParkedVehicle;
                vehicleInfo = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[___m_InstanceID.ParkedVehicle].Info;
                var vehicleFuel = VehicleFuelManager.GetParkedFuelData(vehicleId);
                value = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
            }

            if (vehicleId != 0 && vehicleInfo != null)
            {
                bool isElectric = vehicleInfo.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                Type.text += Environment.NewLine;
                Type.parent.height = 35;
                if (isElectric)
                {
                    Type.text += "Battery Percent:  " + value.ToString("#0%");
                }
                else
                {
                    Type.text += "Fuel Percent:  " + value.ToString("#0%");
                }
            }

        }

        [HarmonyPatch(typeof(CityServiceVehicleWorldInfoPanel), "UpdateBindings")]
        [HarmonyPostfix]
        public static void CityServiceVehicleUpdateBindings(CityServiceVehicleWorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
        {
            var Type = __instance.Find<UILabel>("Type");
            if (Type == null)
            {
                return;
            }
            if (___m_InstanceID.Vehicle != 0 && VehicleFuelManager.FuelDataExist(___m_InstanceID.Vehicle))
            {
                var vehicleFuel = VehicleFuelManager.GetFuelData(___m_InstanceID.Vehicle);
                Type.parent.height = 35;
                Type.text += Environment.NewLine;
                float value = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
                Type.text += "Fuel Percent:  " + value.ToString("#0%");
                var panel = __instance.Find<UIPanel>("(Library) CityServiceVehicleWorldInfoPanel");
                if (panel != null)
                {
                    panel.height = 190;
                }
            }
        }
    }
}