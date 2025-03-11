using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using MoreTransferReasons.AI;
using System;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleAIPatch
    {
        [HarmonyPatch(typeof(VehicleAI), "CreateVehicle")]
        [HarmonyPostfix]
        public static void CreateVehicle(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            CreateFuelForVehicle(__instance, vehicleID, ref data);
        }

        [HarmonyPatch(typeof(VehicleAI), "LoadVehicle")]
        [HarmonyPostfix]
        public static void LoadVehicle(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            CreateFuelForVehicle(__instance, vehicleID, ref data);
        }

        [HarmonyPatch(typeof(VehicleAI), "CalculateTargetSpeed",
            [typeof(ushort), typeof(Vehicle), typeof(float), typeof(float)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void CalculateTargetSpeed(VehicleAI __instance, ushort vehicleID, ref Vehicle data, float speedLimit, float curve, ref float __result)
        {
            if (VehicleFuelManager.VehicleFuelExist(vehicleID) && (__instance is ExtendedPassengerCarAI || __instance is ExtendedCargoTruckAI))
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                if (vehicleFuel.CurrentFuelCapacity < 10)
                {
                    __result = 0.5f;
                }
            }
        }

        [HarmonyPatch(typeof(VehicleAI), "SimulationStep",
            [typeof(ushort), typeof(Vehicle), typeof(Vehicle.Frame), typeof(ushort), typeof(Vehicle), typeof(int)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void SimulationStep(VehicleAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            if (VehicleFuelManager.VehicleFuelExist(vehicleID) && (__instance is ExtendedPassengerCarAI || __instance is ExtendedCargoTruckAI))
            {
                var vehicleFuel = VehicleFuelManager.GetVehicleFuel(vehicleID);
                var CanAskForFuel = false;
                if (vehicleData.m_custom != (ushort)ExtendedTransferManager.TransferReason.FuelVehicle)
                {
                    CanAskForFuel = true;
                }
                if(CanAskForFuel)
                {
                    if(__instance is ExtendedCargoTruckAI)
                    {
                        if (vehicleFuel.OriginalTargetBuilding == 0 && vehicleData.m_targetBuilding != 0)
                        {
                            VehicleFuelManager.SetVehicleFuelOriginalTargetBuilding(vehicleID, vehicleData.m_targetBuilding);
                        }
                    }
                    else if (__instance is ExtendedPassengerCarAI)
                    {
                        ushort driverInstance = GetDriverInstance(vehicleID, ref vehicleData);
                        if (driverInstance != 0)
                        {
                            var targetBuilding = Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_targetBuilding;
                            if (vehicleFuel.OriginalTargetBuilding == 0 && targetBuilding != 0)
                            {
                                VehicleFuelManager.SetVehicleFuelOriginalTargetBuilding(vehicleID, targetBuilding);
                            }
                        }
                    }
                    float percent = vehicleFuel.CurrentFuelCapacity / vehicleFuel.MaxFuelCapacity;
                    bool shouldFuel = Singleton<SimulationManager>.instance.m_randomizer.Int32(32U) == 0;
                    if ((percent > 0.2 && percent < 0.8 && shouldFuel) || percent <= 0.2)
                    {
                        ExtendedTransferManager.Offer offer = default;
                        offer.Vehicle = vehicleID;
                        offer.Position = vehicleData.GetLastFramePosition();
                        offer.Amount = 1;
                        offer.Active = true;
                        Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.FuelVehicle, offer);
                    }
                }
                if (vehicleFuel.CurrentFuelCapacity > 0)
                {
                    VehicleFuelManager.SetVehicleFuel(vehicleID, -0.01f);
                }
            }
        }

        public static void CreateFuelForVehicle(VehicleAI instance, ushort vehicleID, ref Vehicle data)
        {
            if (instance is ExtendedPassengerCarAI && !VehicleFuelManager.VehicleFuelExist(vehicleID))
            {
                ushort driverInstance = GetDriverInstance(vehicleID, ref data);
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(30, 60);
                ushort targetBuilding = 0;
                if (driverInstance != 0)
                {
                    targetBuilding = Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_targetBuilding;
                }
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 60, targetBuilding);
            }
            if (instance is ExtendedCargoTruckAI && !VehicleFuelManager.VehicleFuelExist(vehicleID))
            {
                int randomFuelCapacity = Singleton<SimulationManager>.instance.m_randomizer.Int32(50, 80);
                VehicleFuelManager.CreateVehicleFuel(vehicleID, randomFuelCapacity, 80, data.m_targetBuilding);
            }
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
