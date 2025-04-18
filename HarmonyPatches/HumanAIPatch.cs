using System;
using System.Reflection;
using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class HumanAIPatch
    {
        private delegate void SimulationStepCitizenAIDelegate(CitizenAI instance, ushort instanceID, ref CitizenInstance data, Vector3 physicsLodRefPos);
        private static readonly SimulationStepCitizenAIDelegate SimulationStepCitizenAI = AccessTools.MethodDelegate<SimulationStepCitizenAIDelegate>(typeof(CitizenAI).GetMethod("SimulationStep", BindingFlags.Instance | BindingFlags.Public, null, [typeof(ushort), typeof(CitizenInstance).MakeByRefType(), typeof(Vector3)], null), null, false);

        private delegate void SpawnHumanAIDelegate(HumanAI instance, ushort instanceID, ref CitizenInstance data);
        private static readonly SpawnHumanAIDelegate SpawnHumanAI = AccessTools.MethodDelegate<SpawnHumanAIDelegate>(typeof(HumanAI).GetMethod("Spawn", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

        private delegate void PathfindSuccessHumanAIDelegate(HumanAI instance, ushort instanceID, ref CitizenInstance data);
        private static readonly PathfindSuccessHumanAIDelegate PathfindSuccessHumanAI = AccessTools.MethodDelegate<PathfindSuccessHumanAIDelegate>(typeof(HumanAI).GetMethod("PathfindSuccess", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

        private delegate void PathfindFailureHumanAIDelegate(HumanAI instance, ushort instanceID, ref CitizenInstance data);
        private static readonly PathfindFailureHumanAIDelegate PathfindFailureHumanAI = AccessTools.MethodDelegate<PathfindFailureHumanAIDelegate>(typeof(HumanAI).GetMethod("PathfindFailure", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

        private delegate void InvalidPathCitizenAIDelegate(CitizenAI instance, ushort instanceID, ref CitizenInstance data);
        private static readonly InvalidPathCitizenAIDelegate InvalidPathCitizenAI = AccessTools.MethodDelegate<InvalidPathCitizenAIDelegate>(typeof(CitizenAI).GetMethod("InvalidPath", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        private delegate void ArriveAtDestinationHumanAIDelegate(HumanAI instance, ushort instanceID, ref CitizenInstance citizenData, bool success);
        private static readonly ArriveAtDestinationHumanAIDelegate ArriveAtDestinationHumanAI = AccessTools.MethodDelegate<ArriveAtDestinationHumanAIDelegate>(typeof(HumanAI).GetMethod("ArriveAtDestination", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

        [HarmonyPatch(typeof(HumanAI), "ArriveAtDestination")]
        [HarmonyPrefix]
        public static bool ArriveAtDestination(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool success)
        {
            if(__instance is not ResidentAI && __instance is not TouristAI)
            {
                return true;
            }

            ref var targetBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];

            if(targetBuilding.Info.GetAI() is CarDealerAI || targetBuilding.Info.GetAI() is CarRentalAI)
            {
                if (Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].m_vehicle != 0)
                {
                    Debug.Log("CarRentalAndBuyMod: ReturnRentedVehicle Or SellOwnVehicle");
                    if (VehicleRentalManager.RentalDataExist(citizenData.m_citizen))
                    {
                        VehicleRentalManager.RemoveRentalData(citizenData.m_citizen);
                    }
                    Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, 0, 0u);
                    Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle].Unspawn(citizen.m_vehicle);
                }
                else
                {
                    if (targetBuilding.Info.GetAI() is CarRentalAI carRentalAI && targetBuilding.m_customBuffer1 - carRentalAI.m_rentedCarCount > 0)
                    {
                        Debug.Log("CarRentalAndBuyMod: RentNewVehicle");
                        SpawnVehicleFromShop(__instance, instanceID, ref citizenData, default);
                        carRentalAI.m_rentedCarCount++;
                    }
                    else if (targetBuilding.Info.GetAI() is CarDealerAI && targetBuilding.m_customBuffer1 > 0)
                    {
                        Debug.Log("CarRentalAndBuyMod: BuyNewVehicle");
                        SpawnVehicleFromShop(__instance, instanceID, ref citizenData, default);
                        targetBuilding.m_customBuffer1--;
                    }
                }
                if (CitizenDestinationManager.CitizenDestinationExist(citizenData.m_citizen))
                {
                    var targetBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                    CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);
                    __instance.SetTarget(instanceID, ref citizenData, targetBuildingId);
                }
                else
                {
                    __instance.SetTarget(instanceID, ref citizenData, 0);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HumanAI), "SimulationStep", [typeof(ushort), typeof(CitizenInstance), typeof(Vector3)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool SimulationStep(HumanAI __instance, ushort instanceID, ref CitizenInstance data, Vector3 physicsLodRefPos)
        {
            if ((data.m_flags & (CitizenInstance.Flags.Blown | CitizenInstance.Flags.Floating)) != 0 && (data.m_flags & CitizenInstance.Flags.Character) == 0)
            {
                uint citizen = data.m_citizen;
                Singleton<CitizenManager>.instance.ReleaseCitizenInstance(instanceID);
                if (citizen != 0)
                {
                    Singleton<CitizenManager>.instance.ReleaseCitizen(citizen);
                }
                return false;
            }
            CitizenManager instance = Singleton<CitizenManager>.instance;
            var state = "none";

            if ((data.m_flags & CitizenInstance.Flags.WaitingPath) != 0)
            {
                PathManager instance2 = Singleton<PathManager>.instance;
                byte pathFindFlags = instance2.m_pathUnits.m_buffer[data.m_path].m_pathFindFlags;

                if ((pathFindFlags & PathUnit.FLAG_READY) != 0)
                {
                    state = OnCitizenPathFindSuccess(instanceID, ref data, ref instance.m_citizens.m_buffer[data.m_citizen]);

                    if(state == "ready")
                    {
                        if (data.m_citizen == 0 || instance.m_citizens.m_buffer[data.m_citizen].m_vehicle == 0)
                        {
                            SpawnHumanAI(__instance, instanceID, ref data);
                        }
                        data.m_pathPositionIndex = byte.MaxValue;
                        data.m_flags &= ~CitizenInstance.Flags.WaitingPath;
                        data.m_flags &= ~CitizenInstance.Flags.TargetFlags;
                        PathfindSuccessHumanAI(__instance, instanceID, ref data);
                    }
                    else if(state == "fail_soft")
                    {
                        data.m_flags &= ~CitizenInstance.Flags.WaitingPath;
                        data.m_flags &= ~CitizenInstance.Flags.TargetFlags;
                        InvalidPathCitizenAI(__instance, instanceID, ref data);
                    }
                    else if (state == "fail_hard")
                    {
                        data.m_flags &= ~CitizenInstance.Flags.WaitingPath;
                        data.m_flags &= ~CitizenInstance.Flags.TargetFlags;
                        Singleton<PathManager>.instance.ReleasePath(data.m_path);
                        data.m_path = 0u;
                        PathfindFailureHumanAI(__instance, instanceID, ref data);
                        return false;
                    }
                }
            }

            if (state == "fail_soft")
            {
                return false;
            }

            SimulationStepCitizenAI(__instance, instanceID, ref data, physicsLodRefPos);
            VehicleManager instance3 = Singleton<VehicleManager>.instance;
            ushort num = 0;
            if (data.m_citizen != 0)
            {
                num = instance.m_citizens.m_buffer[data.m_citizen].m_vehicle;
            }
            if (num != 0)
            {
                VehicleInfo info = instance3.m_vehicles.m_buffer[num].Info;
                if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
                {
                    info.m_vehicleAI.SimulationStep(num, ref instance3.m_vehicles.m_buffer[num], num, ref instance3.m_vehicles.m_buffer[num], 0);
                    num = 0;
                }
            }
            if (num == 0 && (data.m_flags & (CitizenInstance.Flags.Character | CitizenInstance.Flags.WaitingPath | CitizenInstance.Flags.Blown | CitizenInstance.Flags.Floating)) == 0)
            {
                data.m_flags &= ~CitizenInstance.Flags.TargetFlags;
                ArriveAtDestinationHumanAI(__instance, instanceID, ref data, success: false);
                instance.ReleaseCitizenInstance(instanceID);
            }

            return false;
        }

        public static bool FindNearByCarShop(Vector3 pos, string buildingAI)
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
                                if (info.GetAI().name == buildingAI)
                                {
                                    if (info.GetAI() is CarDealerAI && building.m_customBuffer1 > 0)
                                    {
                                        return true;
                                    }
                                    else if (info.GetAI() is CarRentalAI carRentalAI && building.m_customBuffer1 - carRentalAI.m_rentedCarCount > 0)
                                    {
                                        return true;
                                    }
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

        public static bool IsRoadConnection(ushort buildingId)
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

        public static bool SpawnVehicle(ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos)
        {
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
                        if (HumanAIPatch.TryJoinVehicle(instanceID, ref citizenData, num6, ref instance.m_vehicles.m_buffer[num6]))
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
            VehicleInfo vehicleInfo = VehicleManagerPatch.GetVehicleInfo(ref citizenData);
            if (vehicleInfo == null || vehicleInfo.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
            {
                if ((citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) == 0)
                {
                    citizenData.m_flags |= CitizenInstance.Flags.TryingSpawnVehicle;
                    citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                    citizenData.m_waitCounter = 0;
                }
                return true;
            }
            if (vehicleInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTaxi)
            {
                if ((citizenData.m_flags & CitizenInstance.Flags.WaitingTaxi) == 0 && instance2.m_segments.m_buffer[pathPos.m_segment].Info.m_hasPedestrianLanes)
                {
                    citizenData.m_flags |= CitizenInstance.Flags.WaitingTaxi;
                    citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                    citizenData.m_waitCounter = 0;
                }
                return true;
            }
            uint laneID = PathManager.GetLaneID(pathPos);
            Vector3 vector2 = citizenData.m_targetPos;
            if (num8 != 0 && Vector3.SqrMagnitude(vector - vector2) < 1024f)
            {
                vector2 = vector;
            }
            else
            {
                num8 = 0;
            }
            instance2.m_lanes.m_buffer[laneID].GetClosestPosition(vector2, out var position, out var laneOffset);
            byte lastPathOffset = (byte)Mathf.Clamp(Mathf.RoundToInt(laneOffset * 255f), 0, 255);
            position = vector2 + Vector3.ClampMagnitude(position - vector2, 5f);
            if (instance.CreateVehicle(out var vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, vector2, TransferManager.TransferReason.None, transferToSource: false, transferToTarget: false))
            {
                var parkedVehicle = instance3.m_citizens.m_buffer[citizenData.m_citizen].m_parkedVehicle;

                if (parkedVehicle != 0 && VehicleRentalManager.RentalDataExist(citizenData.m_citizen))
                {
                    if (VehicleRentalManager.RentalDataExist(citizenData.m_citizen))
                    {
                        Debug.Log("CarRentalAndBuyMod: PassengerCarAI - SetRentalDrivingVehicle");
                        var rental = VehicleRentalManager.GetRentalData(citizenData.m_citizen);
                        rental.RentedVehicleID = vehicle;
                        rental.IsParked = false;
                        VehicleRentalManager.SetRentalData(citizenData.m_citizen, rental);
                    }
                    if (VehicleFuelManager.FuelDataExist(parkedVehicle))
                    {
                        VehicleFuelManager.UpdateParkingMode(parkedVehicle, vehicle, false);
                    }
                }
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
                instance.m_vehicles.m_buffer[vehicle].m_flags |= Vehicle.Flags.Stopped;
                instance.m_vehicles.m_buffer[vehicle].m_path = citizenData.m_path;
                instance.m_vehicles.m_buffer[vehicle].m_pathPositionIndex = citizenData.m_pathPositionIndex;
                instance.m_vehicles.m_buffer[vehicle].m_lastPathOffset = lastPathOffset;
                instance.m_vehicles.m_buffer[vehicle].m_transferSize = (ushort)(citizenData.m_citizen & 0xFFFF);
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
                return true;
            }
            instance3.m_citizens.m_buffer[citizenData.m_citizen].SetParkedVehicle(citizenData.m_citizen, 0);
            if ((citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) == 0)
            {
                citizenData.m_flags |= CitizenInstance.Flags.TryingSpawnVehicle;
                citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                citizenData.m_waitCounter = 0;
            }
            return true;
        }

        public static bool TryJoinVehicle(ushort instanceID, ref CitizenInstance citizenData, ushort vehicleID, ref Vehicle vehicleData)
        {
            if ((vehicleData.m_flags & Vehicle.Flags.Stopped) == 0)
            {
                return false;
            }
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = vehicleData.m_citizenUnits;
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
                        if (instance2 != 0 && instance.m_instances.m_buffer[instance2].m_targetBuilding == citizenData.m_targetBuilding && (instance.m_instances.m_buffer[instance2].m_flags & CitizenInstance.Flags.TargetIsNode) == (citizenData.m_flags & CitizenInstance.Flags.TargetIsNode))
                        {
                            instance.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, vehicleID, 0u);
                            if (instance.m_citizens.m_buffer[citizenData.m_citizen].m_vehicle == vehicleID)
                            {
                                if (citizenData.m_path != 0)
                                {
                                    Singleton<PathManager>.instance.ReleasePath(citizenData.m_path);
                                    citizenData.m_path = 0u;
                                }
                                return true;
                            }
                            break;
                        }
                        break;
                    }
                }
                num = nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return false;
        }

        private static string OnCitizenPathFindSuccess(ushort instanceId, ref CitizenInstance instanceData, ref Citizen citizenData)
        {
            if (citizenData.m_vehicle == 0)
            {
                byte laneTypes = Singleton<PathManager>.instance.m_pathUnits.m_buffer[instanceData.m_path].m_laneTypes;
                uint vehicleTypes = Singleton<PathManager>.instance.m_pathUnits.m_buffer[instanceData.m_path].m_vehicleTypes;

                bool usesCar = (laneTypes & (byte)(NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) != 0 && (vehicleTypes & (ushort)VehicleInfo.VehicleType.Car) != 0;

                if(usesCar && !IsRoadConnection(instanceData.m_sourceBuilding))
                {
                    return "fail_soft";
                }

                return "ready";
            }

            return "fail_hard";
        }

        private static void SpawnVehicleFromShop(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos)
        {
            var original_sourceBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            VehicleManager instance = Singleton<VehicleManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            CitizenManager instance3 = Singleton<CitizenManager>.instance;
            Vector3 vector2 = citizenData.m_targetPos;
            uint laneID = PathManager.GetLaneID(pathPos);
            instance2.m_lanes.m_buffer[laneID].GetClosestPosition(vector2, out var position, out var laneOffset);
            byte lastPathOffset = (byte)Mathf.Clamp(Mathf.RoundToInt(laneOffset * 255f), 0, 255);
            position = vector2 + Vector3.ClampMagnitude(position - vector2, 5f);
            VehicleInfo vehicleInfo = VehicleManagerPatch.GetVehicleInfo(ref citizenData);
            var vehicleCreated = instance.CreateVehicle(out var vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, vector2, TransferManager.TransferReason.None, transferToSource: false, transferToTarget: false);
            if (vehicleCreated)
            {
                if(__instance is TouristAI)
                {
                    VehicleRentalManager.CreateRentalData(citizenData.m_citizen, vehicle, citizenData.m_targetBuilding);
                }
                Vehicle.Frame frameData = instance.m_vehicles.m_buffer[vehicle].m_frame0;
                Vector3 forward = position - citizenData.GetLastFrameData().m_position;
                if (forward.sqrMagnitude > 0.01f)
                {
                    frameData.m_rotation = Quaternion.LookRotation(forward);
                }
                instance.m_vehicles.m_buffer[vehicle].m_frame0 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame1 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame2 = frameData;
                instance.m_vehicles.m_buffer[vehicle].m_frame3 = frameData;
                vehicleInfo.m_vehicleAI.FrameDataUpdated(vehicle, ref instance.m_vehicles.m_buffer[vehicle], ref frameData);
                instance.m_vehicles.m_buffer[vehicle].m_targetPos0 = new Vector4(position.x, position.y, position.z, 2f);
                instance.m_vehicles.m_buffer[vehicle].m_lastPathOffset = lastPathOffset;
                instance.m_vehicles.m_buffer[vehicle].m_flags |= Vehicle.Flags.Stopped;
                instance.m_vehicles.m_buffer[vehicle].m_path = citizenData.m_path;
                instance.m_vehicles.m_buffer[vehicle].m_pathPositionIndex = citizenData.m_pathPositionIndex;
                instance.m_vehicles.m_buffer[vehicle].m_transferSize = (ushort)(citizenData.m_citizen & 0xFFFFu);
                instance.m_vehicles.m_buffer[vehicle].Info.m_vehicleAI.SetSource(vehicle, ref instance.m_vehicles.m_buffer[vehicle], citizenData.m_targetBuilding);
                vehicleInfo.m_vehicleAI.TrySpawn(vehicle, ref instance.m_vehicles.m_buffer[vehicle]);
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
                if (original_sourceBuilding.Info.GetAI() is CarDealerAI)
                {
                    citizenData.m_flags |= CitizenInstance.Flags.TryingSpawnVehicle;
                }
                citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
                citizenData.m_waitCounter = 0;
            }
        }

    }
}
