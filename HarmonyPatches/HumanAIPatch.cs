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
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
            ref var targetBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle];

            if (__instance.m_info.GetAI() is TouristAI touristAI && targetBuilding.Info.GetAI() is CarRentalAI carRentalAI)
            {
                if (VehicleRentalManager.VehicleRentalExist(citizenData.m_citizen))
                {
                    var rental = VehicleRentalManager.GetVehicleRental(citizenData.m_citizen);
                    // i am here to return the car and leave the city
                    if (rental.CarRentalBuildingID == citizenData.m_targetBuilding && CitizenDestinationManager.CitizenDestinationExist(citizenData.m_citizen))
                    {
                        Debug.Log("CarRentalAndBuyMod: TouristAI - ReturnRentalVehicle");
                        // get original outside connection target
                        var targetBuildingId = CitizenDestinationManager.GetCitizenDestination(citizenData.m_citizen);
                        CitizenDestinationManager.RemoveCitizenDestination(citizenData.m_citizen);
                        Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].SetVehicle(citizenData.m_citizen, 0, 0u);
                        VehicleRentalManager.RemoveVehicleRental(citizenData.m_citizen);
                        vehicle.Unspawn(citizen.m_vehicle);
                        // move to outside connection
                        __instance.SetTarget(instanceID, ref citizenData, targetBuildingId);
                        return false;
                    }
                }
                else
                {
                    if (carRentalAI.m_rentedCarCount < targetBuilding.m_customBuffer1)
                    {
                        Debug.Log("CarRentalAndBuyMod: TouristAI - RentNewRentalVehicle");
                        VehicleInfo vehicleInfo = VehicleManagerPatch.GetVehicleInfo(ref citizenData);
                        TouristAIPatch.SpawnRentalVehicle(touristAI, instanceID, ref citizenData, vehicleInfo, default);
                        VehicleRentalManager.CreateVehicleRental(citizenData.m_citizen, citizen.m_vehicle, citizenData.m_sourceBuilding);
                        carRentalAI.m_rentedCarCount++;
                        return false;
                    }
                    return true;
                }
            }
            else if (__instance.m_info.GetAI() is ResidentAI residentAI && targetBuilding.Info.GetAI() is CarDealerAI)
            {
                if (targetBuilding.m_customBuffer1 > 0)
                {
                    Debug.Log("CarRentalAndBuyMod: ResidentAI - BuyNewVehicle");
                    VehicleInfo vehicleInfo = VehicleManagerPatch.GetVehicleInfo(ref citizenData);
                    ResidentAIPatch.SpawnOwnVehicle(residentAI, instanceID, ref citizenData, vehicleInfo, default);
                    targetBuilding.m_customBuffer1--;
                    return false;
                }
                return true;
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

        private static string OnCitizenPathFindSuccess(ushort instanceId, ref CitizenInstance instanceData, ref Citizen citizenData)
        {
            if (citizenData.m_vehicle == 0)
            {
                byte laneTypes = Singleton<PathManager>.instance.m_pathUnits.m_buffer[instanceData.m_path].m_laneTypes;
                uint vehicleTypes = Singleton<PathManager>.instance.m_pathUnits.m_buffer[instanceData.m_path].m_vehicleTypes;

                bool usesCar = (laneTypes & (byte)(NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) != 0 && (vehicleTypes & (ushort)VehicleInfo.VehicleType.Car) != 0;

                if(usesCar)
                {
                    return "fail_soft";
                }

                return "ready";
            }

            return "fail_hard";
        }

    }
}
