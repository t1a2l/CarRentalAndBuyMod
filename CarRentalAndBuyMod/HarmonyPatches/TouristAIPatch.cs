using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoreTransferReasons;
using System;
using System.Reflection;
using UnityEngine;
using static RenderManager;

namespace CarRentalAndBuyMod.HarmonyPatches {

    [HarmonyPatch(typeof(TouristAI))]
    public static class TouristAIPatch
    {
        private delegate bool TryJoinVehicleDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, ushort vehicleID, ref Vehicle vehicleData);
        private static readonly TryJoinVehicleDelegate TryJoinVehicle = AccessTools.MethodDelegate<TryJoinVehicleDelegate>(typeof(TouristAI).GetMethod("TryJoinVehicle", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        private delegate VehicleInfo GetVehicleInfoDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool forceProbability, out VehicleInfo trailer);
        private static readonly GetVehicleInfoDelegate GetVehicleInfo = AccessTools.MethodDelegate<GetVehicleInfoDelegate>(typeof(TouristAI).GetMethod("GetVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        [HarmonyPatch(typeof(TouristAI), "SpawnVehicle")]
        [HarmonyPrefix]
        public static bool SpawnVehicle(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos, ref bool __result)
        {
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_sourceBuilding];
            if(citizenData.m_sourceBuilding != 0 && building.Info.GetAI() is OutsideConnectionAI)
            {
                return true;
            }
            VehicleManager instance = Singleton<VehicleManager>.instance;
            float num = 20f;
            int num2 = Mathf.Max((int)((citizenData.m_targetPos.x - num) / 32f + 270f), 0);
            int num3 = Mathf.Max((int)((citizenData.m_targetPos.z - num) / 32f + 270f), 0);
            int num4 = Mathf.Min((int)((citizenData.m_targetPos.x + num) / 32f + 270f), 539);
            int num5 = Mathf.Min((int)((citizenData.m_targetPos.z + num) / 32f + 270f), 539);
            for (int i = num3; i <= num5; i++)
            {
                for (int j = num2; j <= num4; j++)
                {
                    ushort num6 = instance.m_vehicleGrid[i * 540 + j];
                    int num7 = 0;
                    while (num6 != 0)
                    {
                        if (TryJoinVehicle(__instance, instanceID, ref citizenData, num6, ref instance.m_vehicles.m_buffer[num6]))
                        {
                            citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                            citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
                            citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                            citizenData.m_waitCounter = 0;
                            return true;
                        }
                        num6 = instance.m_vehicles.m_buffer[num6].m_nextGridVehicle;
                        if (++num7 > 16384)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            NetManager instance2 = Singleton<NetManager>.instance;
            CitizenManager instance3 = Singleton<CitizenManager>.instance;
            Vector3 vector = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            ushort num8 = instance3.m_citizens.m_buffer[citizenData.m_citizen].m_parkedVehicle;
            if (num8 != 0)
            {
                vector = instance.m_parkedVehicles.m_buffer[num8].m_position;
                rotation = instance.m_parkedVehicles.m_buffer[num8].m_rotation;
            }
            VehicleInfo vehicleInfo = GetVehicleInfo(__instance, instanceID, ref citizenData, forceProbability: false, out VehicleInfo trailer);
            if (vehicleInfo is null || vehicleInfo.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
            {
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
                if ((citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) == 0)
                {
                    citizenData.m_flags |= CitizenInstance.Flags.TryingSpawnVehicle;
                    citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                    citizenData.m_waitCounter = 0;
                }
                __result = true;
                return false;
            }
            if (vehicleInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTaxi)
            {
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
                if ((citizenData.m_flags & CitizenInstance.Flags.WaitingTaxi) == 0)
                {
                    citizenData.m_flags |= CitizenInstance.Flags.WaitingTaxi;
                    citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                    citizenData.m_waitCounter = 0;
                }
                __result = true;
                return false;
            }
            FindCarRentalPlace(citizenData.m_citizen, citizenData.m_sourceBuilding, ExtendedTransferManager.TransferReason.CarRent);
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(TouristAI), "ArriveAtDestination")]
        [HarmonyPrefix]
        public static void ArriveAtDestination(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool success)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            Building building = instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            if (success && citizenData.m_citizen != 0 && citizenData.m_targetBuilding != 0  && building.Info.GetAI() is CarRentalAI)
            {
                SpawnRentalVehicle(__instance, instanceID, ref citizenData);
            }
        }

        private static void SpawnRentalVehicle(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData)
        {
            VehicleInfo vehicleInfo = GetRentalVehicleInfo(ref citizenData);
            VehicleManager instance = Singleton<VehicleManager>.instance;
            BuildingManager instance2 = Singleton<BuildingManager>.instance;
            CitizenManager instance3 = Singleton<CitizenManager>.instance;
            Building building = instance2.m_buildings.m_buffer[citizenData.m_targetBuilding];
            Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
            if (ExtedndedVehicleManager.CreateVehicle(out var vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, building.m_position, ExtendedTransferManager.TransferReason.CarRent, transferToSource: true, transferToTarget: false))
            {
                ref var data = ref vehicles.m_buffer[vehicle];
                vehicleInfo.m_vehicleAI.SetSource(vehicle, ref data, citizenData.m_targetBuilding);
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].AddOwnVehicle(vehicle, ref vehicles.m_buffer[vehicle]);
                vehicleInfo.m_vehicleAI.TrySpawn(vehicle, ref instance.m_vehicles.m_buffer[vehicle]);
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, vehicle, 0u);
                citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                citizenData.m_waitCounter = 0;
                EnterVehicle(instanceID, ref citizenData);
            }
        }

        private static void FindCarRentalPlace(uint citizenID, ushort sourceBuilding, ExtendedTransferManager.TransferReason reason)
        {
            ExtendedTransferManager.Offer offer = default;
            offer.Citizen = citizenID;
            offer.Position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[sourceBuilding].m_position;
            offer.Amount = 1;
            offer.Active = true;
            Singleton<ExtendedTransferManager>.instance.AddIncomingOffer(reason, offer);
        }

        private static VehicleInfo GetRentalVehicleInfo(ref CitizenInstance citizenData)
        {
            Citizen.Wealth wealthLevel = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].WealthLevel;
            int camper_probability = GetCamperProbability(wealthLevel);
            int car_probability = GetCarProbability(citizenData.m_frame1.m_position);
            int electricCarProbability = GetElectricCarProbability(wealthLevel);
            ItemClass.Service service = ItemClass.Service.Residential;
            ItemClass.SubService subService = ItemClass.SubService.ResidentialLow;
            ItemClass.Level level = ItemClass.Level.Level1;
            Randomizer r = new(citizenData.m_citizen);
            if (car_probability >= camper_probability)
            {
                if(car_probability >= electricCarProbability)
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHigh;
                        level = ItemClass.Level.Level2;
                    }
                }
                else
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHighEco;
                        level = ItemClass.Level.Level2;
                    }
                    else
                    {
                        subService = ItemClass.SubService.ResidentialLowEco;
                        level = ItemClass.Level.Level1;
                    }
                }
            }
            else
            {
                if (camper_probability >= electricCarProbability)
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHigh;
                        level = ItemClass.Level.Level2;
                    }
                }
                else
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHighEco;
                        level = ItemClass.Level.Level2;
                    }
                    else
                    {
                        subService = ItemClass.SubService.ResidentialLowEco;
                        level = ItemClass.Level.Level1;
                    }
                }
            }
            return Singleton<VehicleManager>.instance.GetRandomVehicleInfo(ref r, service, subService, level);
        }

        private static int GetCarProbability(Vector3 position)
        {
            byte park = Singleton<DistrictManager>.instance.GetPark(position);
            if (park != 0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[park].IsAirport && (Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_parkPolicies & DistrictPolicies.Park.CarRentals) != 0)
            {
                return 90;
            }
            return 20;
        }


        private static int GetCamperProbability(Citizen.Wealth wealth)
        {
            return wealth switch
            {
                Citizen.Wealth.Low => 20,
                Citizen.Wealth.Medium => 30,
                Citizen.Wealth.High => 40,
                _ => 0,
            };
        }

        private static int GetElectricCarProbability(Citizen.Wealth wealth)
        {
            return wealth switch
            {
                Citizen.Wealth.Low => 10,
                Citizen.Wealth.Medium => 15,
                Citizen.Wealth.High => 20,
                _ => 0,
            };
        }

        private static bool EnterVehicle(ushort instanceID, ref CitizenInstance citizenData)
        {
            citizenData.m_flags &= ~CitizenInstance.Flags.EnteringVehicle;
            citizenData.Unspawn(instanceID);
            uint citizen = citizenData.m_citizen;
            if (citizen != 0)
            {
                VehicleManager instance = Singleton<VehicleManager>.instance;
                ushort num = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].m_vehicle;
                if (num != 0)
                {
                    num = instance.m_vehicles.m_buffer[num].GetFirstVehicle(num);
                }
                if (num != 0)
                {
                    VehicleInfo info = instance.m_vehicles.m_buffer[num].Info;
                    int ticketPrice = info.m_vehicleAI.GetTicketPrice(num, ref instance.m_vehicles.m_buffer[num]);
                    if (ticketPrice != 0)
                    {
                        Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, ticketPrice, info.m_class);
                    }
                }
            }
            return false;
        }
    }
}