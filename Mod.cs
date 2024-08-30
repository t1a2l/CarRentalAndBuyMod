using CitiesHarmony.API;
using ICities;
using CarRentalAndBuyMod.Managers;
using CarRentalAndBuyMod.Utils;
using System;
using UnityEngine;
using CarRentalAndBuyMod.HarmonyPatches;

namespace CarRentalAndBuyMod
{
    public class Mod : LoadingExtensionBase, IUserMod 
    {
        string IUserMod.Name => "Car Rental And Buy Mod";
        string IUserMod.Description => "Allow citizens to buy and sell vehicles to and from dealerships. allow tourist to rent vehicles while staying in the city";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => PatchUtil.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) PatchUtil.UnpatchAll();
        }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                CitizenDestinationManager.Init();
                VehicleRentalManager.Init();
                GasStationFuelManager.Init();
                VehicleFuelManager.Init();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                CitizenDestinationManager.Deinit();
                VehicleRentalManager.Deinit();
                GasStationFuelManager.Deinit();
                VehicleFuelManager.Deinit();
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            try
            {
                CitizenDestinationManager.Deinit();
                VehicleRentalManager.Deinit();
                GasStationFuelManager.Deinit();
                VehicleFuelManager.Deinit();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }
}
