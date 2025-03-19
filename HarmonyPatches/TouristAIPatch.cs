using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
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
        public static ushort Chosen_Building = 0;

        private delegate bool TryJoinVehicleDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, ushort vehicleID, ref Vehicle vehicleData);
        private static readonly TryJoinVehicleDelegate TryJoinVehicle = AccessTools.MethodDelegate<TryJoinVehicleDelegate>(typeof(TouristAI).GetMethod("TryJoinVehicle", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        private delegate VehicleInfo GetVehicleInfoDelegate(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool forceProbability, out VehicleInfo trailer);
        private static readonly GetVehicleInfoDelegate GetVehicleInfo = AccessTools.MethodDelegate<GetVehicleInfoDelegate>(typeof(TouristAI).GetMethod("GetVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        [HarmonyPatch(typeof(TouristAI), "SetTarget")]
        [HarmonyPrefix]
        public static void SetTargetPrefix(TouristAI __instance, ushort instanceID, ref CitizenInstance data, ushort targetIndex, bool targetIsNode)
        {
            var vehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_vehicle;
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetIndex].Info.GetAI() is GasStationAI && vehicleId != 0 && VehicleFuelManager.VehicleFuelExist(vehicleId))
            {
                VehicleFuelManager.SetVehicleFuelOriginalTargetBuilding(vehicleId, data.m_targetBuilding);
            }
        }

        [HarmonyPatch(typeof(TouristAI), "SetTarget")]
        [HarmonyPostfix]
        public static void SetTargetPostfix(TouristAI __instance, ushort instanceID, ref CitizenInstance data, ushort targetIndex, bool targetIsNode)
        {
            if (IsRoadConnection(data.m_targetBuilding) && data.m_targetBuilding != 0)
            {
                if (VehicleRentalManager.VehicleRentalExist(data.m_citizen) && !CitizenDestinationManager.CitizenDestinationExist(data.m_citizen))
                {
                    Debug.Log("CarRentalAndBuyMod: TouristAI - SetTargetRoadConnection");
                    CitizenDestinationManager.CreateCitizenDestination(data.m_citizen, data.m_targetBuilding);
                    var rental = VehicleRentalManager.GetVehicleRental(data.m_citizen);
                    __instance.SetTarget(instanceID, ref data, rental.CarRentalBuildingID);
                }
            }
        }

        [HarmonyPatch(typeof(TouristAI), "GetLocalizedStatus", [typeof(uint), typeof(Citizen), typeof(InstanceID)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref])]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(uint citizenID, ref Citizen data, ref InstanceID target, ref string __result)
        {
            if (data.m_instance != 0)
            {
                var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[data.m_instance];
                var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding];
                if (targetBuilding.Info.GetAI() is CarDealerAI)
                {
                    target = InstanceID.Empty;
                    __result = "Going to rent a car";
                }
            }
            else
            {
                var visitBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_visitBuilding];
                if (visitBuilding.Info.GetAI() is CarDealerAI)
                {
                    target = InstanceID.Empty;
                    __result = "Renting a car";
                }
            }
        }

        [HarmonyPatch(typeof(TouristAI), "GetColor")]
        [HarmonyPrefix]
        public static bool GetColor(ushort instanceID, ref CitizenInstance data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            if (instanceID == 0)
            {
                return true;
            }

            if (infoMode == InfoManager.InfoMode.Tourism && subInfoMode == InfoManager.SubInfoMode.Attractiveness)
            {
                if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
                {
                    return true;
                }

                if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                {
                    Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
                }

                if(VehicleRentalManager.VehicleRentalExist(data.m_citizen))
                {
                    var rental = VehicleRentalManager.GetVehicleRental(data.m_citizen);
                    if (rental.CarRentalBuildingID == Chosen_Building)
                    {
                        __result = Color.yellow;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                }
                return false;
            }

            return true;
        }

        [HarmonyBefore(["me.tmpe"])]
        [HarmonyPatch(typeof(TouristAI), "GetVehicleInfo")]
        [HarmonyPrefix]
        public static bool GetVehicleInfoPrefix(ushort instanceID, ref CitizenInstance citizenData, bool forceProbability, ref VehicleInfo trailer, ref VehicleInfo __result)
        {
            if (VehicleRentalManager.VehicleRentalExist(citizenData.m_citizen))
            {
                var rental = VehicleRentalManager.GetVehicleRental(citizenData.m_citizen);
                CitizenManager instance3 = Singleton<CitizenManager>.instance;
                ushort vehicle = instance3.m_citizens.m_buffer[citizenData.m_citizen].m_vehicle;
                ushort parked_vehicle = instance3.m_citizens.m_buffer[citizenData.m_citizen].m_parkedVehicle;
                if (vehicle == rental.RentedVehicleID)
                {
                    __result = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[rental.RentedVehicleID].Info;
                    return false;
                }
                else if (parked_vehicle == rental.RentedVehicleID)
                {
                    __result = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[rental.RentedVehicleID].Info;
                    return false;
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
            if (citizenData.m_sourceBuilding != 0 && IsRoadConnection(citizenData.m_sourceBuilding))
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
            ushort vehicle = instance3.m_citizens.m_buffer[citizenData.m_citizen].m_vehicle;
            ushort parked_vehicle = instance3.m_citizens.m_buffer[citizenData.m_citizen].m_parkedVehicle;
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

            // tourist have a rental vehicle spawn it and use it and set the vehicle instead of the parked vehicle
            if(VehicleRentalManager.VehicleRentalExist(citizenData.m_citizen))
            {
                var rental = VehicleRentalManager.GetVehicleRental(citizenData.m_citizen);
                // edge case
                if (rental.RentedVehicleID == parked_vehicle && sourceBuilding.Info.GetAI() is CarRentalAI)
                {
                    __result = true;
                    return false;
                }

                var parkedVehicleFuel = VehicleFuelManager.GetParkedVehicleFuel(parked_vehicle);
                VehicleRentalManager.SetVehicleRental(citizenData.m_citizen, rental);
                Debug.Log("CarRentalAndBuyMod: TouristAI - SpawnVehicleHasRental");
                SpawnRentalVehicle(__instance, instanceID, ref citizenData, vehicleInfo, pathPos);
                Citizen citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
                rental.RentedVehicleID = citizen.m_vehicle;
                VehicleRentalManager.SetVehicleRental(citizenData.m_citizen, rental);
                
                VehicleFuelManager.CreateVehicleFuel(citizen.m_vehicle, parkedVehicleFuel.CurrentFuelCapacity, parkedVehicleFuel.MaxFuelCapacity, 0);
                VehicleFuelManager.RemoveParkedVehicleFuel(parked_vehicle);
                __result = true;
                return false;
            }

            // do not find a rental place if you are leaving the city or have a rented vehicle
            if (!IsRoadConnection(citizenData.m_targetBuilding) && !VehicleRentalManager.VehicleRentalExist(citizenData.m_citizen) && 
                FindCarRentals(citizenData.m_frame0.m_position) && !CitizenDestinationManager.CitizenDestinationExist(citizenData.m_citizen))
            {
                CitizenDestinationManager.CreateCitizenDestination(citizenData.m_citizen, citizenData.m_targetBuilding);
                FindCarRentalPlace(citizenData.m_citizen, citizenData.m_sourceBuilding, ExtendedTransferManager.TransferReason.CarRent);
                __result = false;
                return false;
            }
            __result = false;
            return false;
        }

        public static void SpawnRentalVehicle(TouristAI __instance, ushort instanceID, ref CitizenInstance citizenData, VehicleInfo vehicleInfo, PathUnit.Position pathPos)
        {
            var original_sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            VehicleManager instance = Singleton<VehicleManager>.instance;
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
            Vector3 vector2 = citizenData.m_targetPos;
            if (num8 != 0 && Vector3.SqrMagnitude(vector - vector2) < 1024f)
            {
                vector2 = vector;
            }
            else
            {
                num8 = 0;
            }
            uint laneID = PathManager.GetLaneID(pathPos);
            instance2.m_lanes.m_buffer[laneID].GetClosestPosition(vector2, out var position, out var laneOffset);
            byte lastPathOffset = (byte)Mathf.Clamp(Mathf.RoundToInt(laneOffset * 255f), 0, 255);
            position = vector2 + Vector3.ClampMagnitude(position - vector2, 5f);
            var vehicleCreated = instance.CreateVehicle(out var vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, vector2, TransferManager.TransferReason.None, transferToSource: false, transferToTarget: false);
            if (vehicleCreated && CitizenDestinationManager.CitizenDestinationExist(citizenData.m_citizen))
            {
                var targeBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                if (targeBuildingId != 0)
                {
                    Debug.Log("CarRentalAndBuyMod: TouristAI - SpawnRentalVehicleSpecial");
                    ref Vehicle data = ref instance.m_vehicles.m_buffer[vehicle];
                    data.Info.m_vehicleAI.SetSource(vehicle, ref data, citizenData.m_targetBuilding);
                    citizenData.m_sourceBuilding = citizenData.m_targetBuilding;
                    CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);
                    __instance.SetTarget(instanceID, ref citizenData, targeBuildingId, false);
                }
                else
                {
                    Debug.Log("CarRentalAndBuyMod: TouristAI - SpawnRentalVehicleNormal");
                    Vehicle.Frame frameData = instance.m_vehicles.m_buffer[vehicle].m_frame0;
                    if (num8 != 0)
                    {
                        frameData.m_rotation = rotation;
                    }
                    else
                    {
                        Vector3 forward = position - citizenData.GetLastFrameData().m_position;
                        if (forward.sqrMagnitude > 0.01f)
                        {
                            frameData.m_rotation = Quaternion.LookRotation(forward);
                        }
                    }
                    instance.m_vehicles.m_buffer[vehicle].m_frame0 = frameData;
                    instance.m_vehicles.m_buffer[vehicle].m_frame1 = frameData;
                    instance.m_vehicles.m_buffer[vehicle].m_frame2 = frameData;
                    instance.m_vehicles.m_buffer[vehicle].m_frame3 = frameData;
                    vehicleInfo.m_vehicleAI.FrameDataUpdated(vehicle, ref instance.m_vehicles.m_buffer[vehicle], ref frameData);
                    instance.m_vehicles.m_buffer[vehicle].m_targetPos0 = new Vector4(position.x, position.y, position.z, 2f);
                    instance.m_vehicles.m_buffer[vehicle].m_lastPathOffset = lastPathOffset;
                }
                
                instance.m_vehicles.m_buffer[vehicle].m_flags |= Vehicle.Flags.Stopped;
                instance.m_vehicles.m_buffer[vehicle].m_path = citizenData.m_path;
                instance.m_vehicles.m_buffer[vehicle].m_pathPositionIndex = citizenData.m_pathPositionIndex;
                instance.m_vehicles.m_buffer[vehicle].m_transferSize = (ushort)(citizenData.m_citizen & 0xFFFFu);
                vehicleInfo.m_vehicleAI.TrySpawn(vehicle, ref instance.m_vehicles.m_buffer[vehicle]);
                if (num8 != 0)
                {
                    InstanceID empty = InstanceID.Empty;
                    empty.ParkedVehicle = num8;
                    InstanceID empty2 = InstanceID.Empty;
                    empty2.Vehicle = vehicle;
                    Singleton<InstanceManager>.instance.ChangeInstance(empty, empty2);
                }
                citizenData.m_path = 0u;
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
                instance3.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, vehicle, 0u);
                citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                citizenData.m_waitCounter = 0;
            }
            instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
            if ((citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) == 0)
            {
                if(original_sourceBuilding.Info.GetAI() is CarRentalAI)
                {
                    citizenData.m_flags |= CitizenInstance.Flags.TryingSpawnVehicle;
                }
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
                                Building building = instance.m_buildings.m_buffer[num12];
                                BuildingInfo info = instance.m_buildings.m_buffer[num12].Info;
                                if (info.GetAI() is CarRentalAI carRentalAI && carRentalAI.m_rentedCarCount < building.m_customBuffer1)
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

        private static bool IsRoadConnection(ushort buildingId)
        {
            if (buildingId != 0)
            {
                BuildingManager instance = Singleton<BuildingManager>.instance;
                var building = instance.m_buildings.m_buffer[buildingId];

                if (building.Info.GetAI() is OutsideConnectionAI && (building.m_flags & Building.Flags.IncomingOutgoing) != 0 && building.Info.m_class.m_service == ItemClass.Service.Road)
                {
                    return true;
                }
            }
            return false;
        }

    }
}