using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoreTransferReasons;
using System;
using System.Reflection;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{

    [HarmonyPatch]
    public static class TouristAIPatch
    {
        private delegate bool TryJoinVehicleDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, ushort vehicleID, ref Vehicle vehicleData);
        private static readonly TryJoinVehicleDelegate TryJoinVehicle = AccessTools.MethodDelegate<TryJoinVehicleDelegate>(typeof(TouristAI).GetMethod("TryJoinVehicle", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        private delegate VehicleInfo GetVehicleInfoDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool forceProbability, out VehicleInfo trailer);
        private static readonly GetVehicleInfoDelegate GetVehicleInfo = AccessTools.MethodDelegate<GetVehicleInfoDelegate>(typeof(TouristAI).GetMethod("GetVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        [HarmonyPatch(typeof(CitizenAI), "StartPathFind", 
            [typeof(ushort), typeof(CitizenInstance), typeof(Vector3), typeof(Vector3), typeof(VehicleInfo), typeof(bool), typeof(bool)], 
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void StartPathFindPrefix(CitizenAI __instance, ushort instanceID, ref CitizenInstance citizenData, ref VehicleInfo vehicleInfo)
        {
            if(__instance is TouristAI)
            {
                var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
                var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
                if (targetBuilding.Info.GetAI() is CarRentalAI && citizen.m_vehicle == 0)
                {
                    vehicleInfo = null;
                }
            }
        }

        [HarmonyPatch(typeof(TouristAI), "SetTarget")]
        [HarmonyPostfix]
        public static void SetTarget(TouristAI __instance, ushort instanceID, ref CitizenInstance data, ushort targetIndex, bool targetIsNode)
        {
            var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen];
            if (data.m_targetBuilding != 0 && citizen.m_vehicle != 0)
            {
                var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle];
                var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                var car_source_building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[vehicle.m_sourceBuilding];
                if (targetBuilding.Info.GetAI() is OutsideConnectionAI && vehicle.m_sourceBuilding != 0 && car_source_building.Info.GetAI() is CarRentalAI)
                {
                    CitizenDestinationManager.CreateCitizenDestination(data.m_citizen, data.m_targetBuilding);
                    __instance.SetTarget(instanceID, ref data, vehicle.m_sourceBuilding);
                }
            }
        }

        [HarmonyPatch(typeof(HumanAI), "ArriveAtDestination")]
        [HarmonyPrefix]
        public static bool ArriveAtDestination(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool success)
        {
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
            var sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_sourceBuilding];
            var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle];
            
            if (__instance.m_info.GetAI() is TouristAI touristAI && targetBuilding.Info.GetAI() is CarRentalAI carRentalAI)
            {
                // i am here to return the car and leave the city
                if (citizen.m_vehicle != 0 && vehicle.m_sourceBuilding == citizenData.m_targetBuilding)
                {
                    // get original outside connection target
                    var targeBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                    CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);

                    Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, 0, 0u);
                    Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle].m_sourceBuilding = 0;
                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].RemoveOwnVehicle(citizen.m_vehicle, ref vehicle);
                    vehicle.Unspawn(citizen.m_vehicle);

                    // move to outside connection
                    touristAI.StartMoving(citizenData.m_citizen, ref citizen, citizenData.m_targetBuilding, targeBuildingId);
                    return false;
                }
                else
                {
                    if(carRentalAI.m_rentedCarCount < carRentalAI.m_rentalCarCount)
                    {
                        SpawnRentalVehicle(touristAI, instanceID, ref citizenData);
                        return false;
                    }
                    return true;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(TouristAI), "SpawnVehicle")]
        [HarmonyPrefix]
        public static bool SpawnVehicle(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos, ref bool __result)
        {
            var sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_sourceBuilding];
            var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            // if you come from outside from road connection you can spawn a car
            if (citizenData.m_sourceBuilding != 0 && sourceBuilding.Info.GetAI() is OutsideConnectionAI && sourceBuilding.Info.name.Contains("Road"))
            {
                return true;
            }
            // if you exit the rental building and not leaving the city get a rental car
            if (citizenData.m_sourceBuilding != 0 && sourceBuilding.Info.GetAI() is CarRentalAI && targetBuilding.Info.GetAI() is not OutsideConnectionAI)
            {
                SpawnRentalVehicle(__instance, instanceID, ref citizenData);
                __result = true;
                return false;
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
            // do not find a rental place if you are leaving the city
            if (targetBuilding.Info.GetAI() is not OutsideConnectionAI && FindCarRentals(citizenData.m_frame0.m_position))
            {
                CitizenDestinationManager.CreateCitizenDestination(citizenData.m_citizen, citizenData.m_targetBuilding);
                FindCarRentalPlace(citizenData.m_citizen, citizenData.m_sourceBuilding, ExtendedTransferManager.TransferReason.CarRent);
                __result = false;
                return false;
            }
            __result = false;
            return false;
        }

        private static void SpawnRentalVehicle(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            CitizenManager instance3 = Singleton<CitizenManager>.instance;
            Vector3 vector2 = citizenData.m_targetPos;
            VehicleInfo vehicleInfo = GetRentalVehicleInfo(ref citizenData);
            if (ExtedndedVehicleManager.CreateVehicle(out var vehicleId, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, vector2, ExtendedTransferManager.TransferReason.None, transferToSource: false, transferToTarget: false))
            {
                ref Vehicle data = ref instance.m_vehicles.m_buffer[vehicleId];
                ref Citizen citizen = ref instance3.m_citizens.m_buffer[citizenData.m_citizen];

                data.m_sourceBuilding = citizenData.m_targetBuilding;
                data.Info.m_vehicleAI.SetSource(vehicleId, ref data, citizenData.m_targetBuilding);
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].AddOwnVehicle(vehicleId, ref data);

                var targeBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);
                __instance.SetTarget(instanceID, ref citizenData, targeBuildingId, false);

                data.m_flags |= Vehicle.Flags.Stopped;
                data.m_path = citizenData.m_path;
                data.m_pathPositionIndex = citizenData.m_pathPositionIndex;
                data.m_transferSize = (ushort)(citizenData.m_citizen & 0xFFFFu);

                vehicleInfo.m_vehicleAI.TrySpawn(vehicleId, ref data);
                citizen.SetParkedVehicle(citizenData.m_citizen, 0);
                citizen.SetVehicle(citizenData.m_citizen, vehicleId, 0);
                citizenData.m_path = 0u;
                citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                citizenData.m_flags &= ~CitizenInstance.Flags.WaitingPath;
                citizenData.m_flags &= ~CitizenInstance.Flags.SittingDown;
                citizenData.m_waitCounter = 0;

                ref var rental_building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding];
                CarRentalAI carRentalAI = rental_building.Info.m_buildingAI as CarRentalAI;
                carRentalAI.m_rentedCarCount++;
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

        private static bool FindCarRentals(Vector3 pos)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            uint numBuildings = instance.m_buildings.m_size;
            int num = Mathf.Max((int)(pos.x / 64f + 135f), 0);
            int num2 = Mathf.Max((int)(pos.z / 64f + 135f), 0);
            int num3 = Mathf.Min((int)(pos.x / 64f + 135f), 269);
            int num4 = Mathf.Min((int)(pos.z / 64f + 135f), 269);
            int num5 = num + 1;
            int num6 = num2 + 1;
            int num7 = num3 - 1;
            int num8 = num4 - 1;
            ushort num9 = 0;
            float num10 = 1E+12f;
            float num11 = 0f;
            while (num != num5 || num2 != num6 || num3 != num7 || num4 != num8)
            {
                for (int i = num2; i <= num4; i++)
                {
                    for (int j = num; j <= num3; j++)
                    {
                        if (j >= num5 && i >= num6 && j <= num7 && i <= num8)
                        {
                            j = num7;
                            continue;
                        }
                        ushort num12 = instance.m_buildingGrid[i * 270 + j];
                        int num13 = 0;
                        while (num12 != 0)
                        {
                            if ((instance.m_buildings.m_buffer[num12].m_flags & (Building.Flags.Created | Building.Flags.Deleted | Building.Flags.Untouchable | Building.Flags.Collapsed)) == Building.Flags.Created && instance.m_buildings.m_buffer[num12].m_fireIntensity == 0 && instance.m_buildings.m_buffer[num12].GetLastFrameData().m_fireDamage == 0)
                            {
                                BuildingInfo info = instance.m_buildings.m_buffer[num12].Info;
                                if (info.GetAI() is CarRentalAI carRentalAI && carRentalAI.m_rentedCarCount < carRentalAI.m_rentalCarCount)
                                {
                                    return true;
                                }
                            }
                            num12 = instance.m_buildings.m_buffer[num12].m_nextGridBuilding;
                            if (++num13 >= numBuildings)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                }
                if (num9 != 0 && num10 <= num11 * num11)
                {
                    return false;
                }
                num11 += 64f;
                num5 = num;
                num6 = num2;
                num7 = num3;
                num8 = num4;
                num = Mathf.Max(num - 1, 0);
                num2 = Mathf.Max(num2 - 1, 0);
                num3 = Mathf.Min(num3 + 1, 269);
                num4 = Mathf.Min(num4 + 1, 269);
            }
            return false;
        }

    }
}