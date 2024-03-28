using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using System;
using System.Reflection;
using UnityEngine;

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
            VehicleInfo vehicleInfo = GetVehicleInfo(__instance, instanceID, ref citizenData, forceProbability: false, out VehicleInfo trailer);
            VehicleManager instance = Singleton<VehicleManager>.instance;
            BuildingManager instance2 = Singleton<BuildingManager>.instance;
            CitizenManager instance3 = Singleton<CitizenManager>.instance;
            Building building = instance2.m_buildings.m_buffer[citizenData.m_targetBuilding];
            Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
            if (ExtedndedVehicleManager.CreateVehicle(out var vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, building.m_position, ExtendedTransferManager.TransferReason.CarRent, transferToSource: false, transferToTarget: false))
            {
                Vehicle.Frame frameData = instance.m_vehicles.m_buffer[vehicle].m_frame0;
                instance.m_vehicles.m_buffer[vehicle].m_frame0 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame1 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame2 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame3 = frameData;
                vehicleInfo.m_vehicleAI.FrameDataUpdated(vehicle, ref instance.m_vehicles.m_buffer[vehicle], ref frameData);
                
                instance.m_vehicles.m_buffer[vehicle].m_flags |= Vehicle.Flags.Stopped;
                instance.m_vehicles.m_buffer[vehicle].m_path = citizenData.m_path;
                instance.m_vehicles.m_buffer[vehicle].m_pathPositionIndex = citizenData.m_pathPositionIndex;
                instance.m_vehicles.m_buffer[vehicle].m_transferSize = (ushort)(citizenData.m_citizen & 0xFFFFu);
                if (trailer != null)
                {
                    instance.m_vehicles.m_buffer[vehicle].CreateTrailer(vehicle, trailer, invert: false);
                }
                vehicleInfo.m_vehicleAI.TrySpawn(vehicle, ref instance.m_vehicles.m_buffer[vehicle]);
                citizenData.m_path = 0u;
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, vehicle, 0u);
                citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                citizenData.m_waitCounter = 0;
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].AddOwnVehicle(vehicle, ref vehicles.m_buffer[vehicle]);
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

    }
}