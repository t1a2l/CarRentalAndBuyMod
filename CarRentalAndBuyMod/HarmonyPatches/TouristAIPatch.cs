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

    [HarmonyPatch]
    public static class TouristAIPatch
    {
        private delegate bool TryJoinVehicleDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, ushort vehicleID, ref Vehicle vehicleData);
        private static readonly TryJoinVehicleDelegate TryJoinVehicle = AccessTools.MethodDelegate<TryJoinVehicleDelegate>(typeof(TouristAI).GetMethod("TryJoinVehicle", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        private delegate VehicleInfo GetVehicleInfoDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool forceProbability, out VehicleInfo trailer);
        private static readonly GetVehicleInfoDelegate GetVehicleInfo = AccessTools.MethodDelegate<GetVehicleInfoDelegate>(typeof(TouristAI).GetMethod("GetVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

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
                    __instance.SetTarget(instanceID, ref data, vehicle.m_sourceBuilding);
                }
            }
        }

        [HarmonyPatch(typeof(HumanAI), "ArriveAtDestination")]
        [HarmonyPrefix]
        public static bool ArriveAtDestination(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool success)
        {
            var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
            var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle];
            // i am here to return the car and leave the city
            if (__instance.m_info.GetAI() is TouristAI && targetBuilding.Info.GetAI() is CarRentalAI && citizen.m_vehicle != 0 && vehicle.m_sourceBuilding == citizenData.m_targetBuilding)
            {
                Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, 0, 0u);
                Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle].m_sourceBuilding = 0;
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].RemoveOwnVehicle(citizen.m_vehicle, ref vehicle);
                vehicle.Unspawn(citizen.m_vehicle);
                FindVisitPlace(citizenData.m_citizen, citizenData.m_targetBuilding, GetLeavingReason(citizenData.m_citizen, ref citizen));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(TouristAI), "SpawnVehicle")]
        [HarmonyPrefix]
        public static bool SpawnVehicle(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos, ref bool __result)
        {
            var sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_sourceBuilding];
            var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            // if you come from outside you can spawn a car
            if (citizenData.m_sourceBuilding != 0 && sourceBuilding.Info.GetAI() is OutsideConnectionAI)
            {
                return true;
            }
            // if you exit the rental building and not leaving the city get a rental car
            if (citizenData.m_sourceBuilding != 0 && sourceBuilding.Info.GetAI() is CarRentalAI && targetBuilding.Info.GetAI() is not OutsideConnectionAI)
            {
                SpawnRentalVehicle(__instance, instanceID, ref citizenData, pathPos);
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
            if (targetBuilding.Info.GetAI() is not OutsideConnectionAI)
            {
                FindCarRentalPlace(citizenData.m_citizen, citizenData.m_sourceBuilding, ExtendedTransferManager.TransferReason.CarRent);
                __result = false;
                return false;
            }
            __result = false;
            return false;
        }

        private static void SpawnRentalVehicle(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            CitizenManager instance3 = Singleton<CitizenManager>.instance;
            Vector3 vector2 = citizenData.m_targetPos;
            uint laneID = PathManager.GetLaneID(pathPos);
            instance2.m_lanes.m_buffer[laneID].GetClosestPosition(vector2, out var position, out var laneOffset);
            byte lastPathOffset = (byte)Mathf.Clamp(Mathf.RoundToInt(laneOffset * 255f), 0, 255);
            position = vector2 + Vector3.ClampMagnitude(position - vector2, 5f);
            VehicleInfo vehicleInfo = GetRentalVehicleInfo(ref citizenData);
            if (ExtedndedVehicleManager.CreateVehicle(out var vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, vector2, ExtendedTransferManager.TransferReason.None, transferToSource: false, transferToTarget: false))
            {
                Vehicle.Frame frameData = instance.m_vehicles.m_buffer[vehicle].m_frame0;
                instance.m_vehicles.m_buffer[vehicle].m_frame0 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame1 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame2 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame3 = frameData;
                vehicleInfo.m_vehicleAI.FrameDataUpdated(vehicle, ref instance.m_vehicles.m_buffer[vehicle], ref frameData);
                instance.m_vehicles.m_buffer[vehicle].m_targetPos0 = new Vector4(position.x, position.y, position.z, 2f);
                instance.m_vehicles.m_buffer[vehicle].m_flags |= Vehicle.Flags.Stopped;
                instance.m_vehicles.m_buffer[vehicle].m_path = citizenData.m_path;
                instance.m_vehicles.m_buffer[vehicle].m_pathPositionIndex = citizenData.m_pathPositionIndex;
                instance.m_vehicles.m_buffer[vehicle].m_lastPathOffset = lastPathOffset;
                instance.m_vehicles.m_buffer[vehicle].m_transferSize = (ushort)(citizenData.m_citizen & 0xFFFFu);
                instance.m_vehicles.m_buffer[vehicle].m_sourceBuilding = citizenData.m_sourceBuilding;
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_sourceBuilding].AddOwnVehicle(vehicle, ref instance.m_vehicles.m_buffer[vehicle]);
                vehicleInfo.m_vehicleAI.TrySpawn(vehicle, ref instance.m_vehicles.m_buffer[vehicle]);
                citizenData.m_path = 0u;
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, vehicle, 0u);
                citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                citizenData.m_waitCounter = 0;
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

        private static TransferManager.TransferReason GetLeavingReason(uint citizenID, ref Citizen data)
        {
            return data.WealthLevel switch
            {
                Citizen.Wealth.Low => TransferManager.TransferReason.LeaveCity0,
                Citizen.Wealth.Medium => TransferManager.TransferReason.LeaveCity1,
                Citizen.Wealth.High => TransferManager.TransferReason.LeaveCity2,
                _ => TransferManager.TransferReason.LeaveCity0,
            };
        }

        private static void FindVisitPlace(uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason)
        {
            TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
            offer.Priority = Singleton<SimulationManager>.instance.m_randomizer.Int32(8u);
            offer.Citizen = citizenID;
            offer.Position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[sourceBuilding].m_position;
            offer.Amount = 1;
            offer.Active = true;
            Singleton<TransferManager>.instance.AddIncomingOffer(reason, offer);
        }
    }
}