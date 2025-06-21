using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace CarRentalAndBuyMod.AI
{
    public class CustomCargoTruckAI : CargoTruckAI
    {
        public static bool CustomStartPathFind(ushort vehicleID, ref Vehicle vehicleData)
        {
            var m_info = vehicleData.Info;
            if ((vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != 0)
            {
                return true;
            }
            if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                if (vehicleData.m_sourceBuilding != 0)
                {
                    BuildingManager instance = Singleton<BuildingManager>.instance;
                    BuildingInfo info = instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding].Info;
                    Randomizer randomizer = new(vehicleID);
                    info.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_sourceBuilding, ref instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding], ref randomizer, m_info, out Vector3 a, out Vector3 target);
                    return StartPathFindCustom(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target, true, true, false, false, false);
                }
            }
            else if (vehicleData.m_targetBuilding != 0)
            {
                BuildingManager instance2 = Singleton<BuildingManager>.instance;
                BuildingInfo info2 = instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding].Info;
                Randomizer randomizer2 = new(vehicleID);
                info2.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_targetBuilding, ref instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding], ref randomizer2, m_info, out Vector3 b, out Vector3 target2);
                return StartPathFindCustom(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target2, true, true, false, false, false);
            }
            return false;
        }


        private static bool StartPathFindCustom(CargoTruckAI __instance, ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays, bool undergroundTarget, bool startNotUseTruck, bool endNotUseTruck)
        {
            if ((vehicleData.m_flags & (Vehicle.Flags.TransferToSource | Vehicle.Flags.GoingBack)) != 0)
            {
                return CarAIStartPathFind(__instance, vehicleID, ref vehicleData, startPos, endPos, startBothWays, endBothWays, undergroundTarget);
            }
            bool allowUnderground = (vehicleData.m_flags & (Vehicle.Flags.Underground | Vehicle.Flags.Transition)) != 0;
            bool flag = PathManager.FindPathPosition(startPos, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, VehicleInfo.VehicleType.Car, vehicleData.Info.vehicleCategory, allowUnderground, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: false, out PathUnit.Position pathPosA, out PathUnit.Position pathPosB, out float distanceSqrA, out float distanceSqrB) && !startNotUseTruck;
            if (PathManager.FindPathPosition(startPos, ItemClass.Service.PublicTransport, NetInfo.LaneType.Vehicle, VehicleInfo.VehicleType.Train | VehicleInfo.VehicleType.Ship | VehicleInfo.VehicleType.Plane, vehicleData.Info.vehicleCategory | VehicleInfo.VehicleCategory.Cargo, allowUnderground, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: false, out var pathPosA2, out var pathPosB2, out var distanceSqrA2, out var distanceSqrB2))
            {
                if (!flag || (distanceSqrA2 < distanceSqrA && (Mathf.Abs(startPos.x) > 4800f || Mathf.Abs(startPos.z) > 4800f)))
                {
                    pathPosA = pathPosA2;
                    pathPosB = pathPosB2;
                    distanceSqrA = distanceSqrA2;
                    distanceSqrB = distanceSqrB2;
                }
                flag = true;
            }
            bool flag2 = PathManager.FindPathPosition(endPos, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, VehicleInfo.VehicleType.Car, vehicleData.Info.vehicleCategory, undergroundTarget, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: false, out PathUnit.Position pathPosA3, out PathUnit.Position pathPosB3, out float distanceSqrA3, out float distanceSqrB3) && !endNotUseTruck;
            if (PathManager.FindPathPosition(endPos, ItemClass.Service.PublicTransport, NetInfo.LaneType.Vehicle, VehicleInfo.VehicleType.Train | VehicleInfo.VehicleType.Ship | VehicleInfo.VehicleType.Plane, vehicleData.Info.vehicleCategory | VehicleInfo.VehicleCategory.Cargo, undergroundTarget, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: false, out var pathPosA4, out var pathPosB4, out var distanceSqrA4, out var distanceSqrB4))
            {
                if (!flag2 || (distanceSqrA4 < distanceSqrA3 && (Mathf.Abs(endPos.x) > 4800f || Mathf.Abs(endPos.z) > 4800f)))
                {
                    pathPosA3 = pathPosA4;
                    pathPosB3 = pathPosB4;
                    distanceSqrA3 = distanceSqrA4;
                    distanceSqrB3 = distanceSqrB4;
                }
                flag2 = true;
            }
            if (flag && flag2)
            {
                PathManager instance = Singleton<PathManager>.instance;
                if (!startBothWays || distanceSqrA < 10f)
                {
                    pathPosB = default;
                }
                if (!endBothWays || distanceSqrA3 < 10f)
                {
                    pathPosB3 = default;
                }
                var randpomParking = false;
                // if the vehicle is a passenger car and the original target building is the gas station (same as the current target), then we can enable random parking
                if (vehicleData.Info.GetAI() is PassengerCarAI)
                {
                    var vehicleFuel = VehicleFuelManager.GetFuelData(vehicleID);
                    if(vehicleFuel.OriginalTargetBuilding != 0 && vehicleFuel.OriginalTargetBuilding == vehicleData.m_targetBuilding)
                    {
                        randpomParking = true;
                    }
                }
                NetInfo.LaneType laneTypes = NetInfo.LaneType.Vehicle | NetInfo.LaneType.CargoVehicle;
                VehicleInfo.VehicleType vehicleTypes = VehicleInfo.VehicleType.Car | VehicleInfo.VehicleType.Train | VehicleInfo.VehicleType.Ship | VehicleInfo.VehicleType.Plane;
                VehicleInfo.VehicleCategory vehicleCategories = vehicleData.Info.vehicleCategory | VehicleInfo.VehicleCategory.Cargo;
                if (instance.CreatePath(out var unit, ref Singleton<SimulationManager>.instance.m_randomizer, Singleton<SimulationManager>.instance.m_currentBuildIndex, pathPosA, pathPosB, pathPosA3, pathPosB3, default(PathUnit.Position), laneTypes, vehicleTypes, vehicleCategories, 20000f, __instance.m_isHeavyVehicle, false, stablePath: false, skipQueue: false, randpomParking, ignoreFlooded: false, true))
                {
                    if (vehicleData.m_path != 0)
                    {
                        instance.ReleasePath(vehicleData.m_path);
                    }
                    vehicleData.m_path = unit;
                    vehicleData.m_flags |= Vehicle.Flags.WaitingPath;
                    return true;
                }
            }
            return false;
        }


        private static bool CarAIStartPathFind(CargoTruckAI instance, ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays, bool undergroundTarget)
        {
            VehicleInfo info = instance.m_info;
            bool allowUnderground = (vehicleData.m_flags & (Vehicle.Flags.Underground | Vehicle.Flags.Transition)) != 0;
            if (PathManager.FindPathPosition(startPos, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, info.m_vehicleType, info.vehicleCategory, allowUnderground, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: false, out var pathPosA, out var pathPosB, out var distanceSqrA, out var _) && PathManager.FindPathPosition(endPos, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, info.m_vehicleType, info.vehicleCategory, undergroundTarget, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: false, out var pathPosA2, out var pathPosB2, out var distanceSqrA2, out var _))
            {
                if (!startBothWays || distanceSqrA < 10f)
                {
                    pathPosB = default;
                }
                if (!endBothWays || distanceSqrA2 < 10f)
                {
                    pathPosB2 = default;
                }
                if (Singleton<PathManager>.instance.CreatePath(out var unit, ref Singleton<SimulationManager>.instance.m_randomizer, Singleton<SimulationManager>.instance.m_currentBuildIndex, pathPosA, pathPosB, pathPosA2, pathPosB2, default(PathUnit.Position), NetInfo.LaneType.Vehicle, info.m_vehicleType, info.vehicleCategory, 20000f, instance.m_isHeavyVehicle, false, stablePath: false, skipQueue: false, randomParking: false, ignoreFlooded: false, true))
                {
                    if (vehicleData.m_path != 0)
                    {
                        Singleton<PathManager>.instance.ReleasePath(vehicleData.m_path);
                    }
                    vehicleData.m_path = unit;
                    vehicleData.m_flags |= Vehicle.Flags.WaitingPath;
                    return true;
                }
            }
            return false;
        }

    }
}
