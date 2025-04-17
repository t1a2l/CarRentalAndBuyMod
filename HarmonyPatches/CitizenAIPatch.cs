using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Managers;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class CitizenAIPatch
    {
        [HarmonyPatch(typeof(CitizenAI), "StartPathFind", [typeof(ushort), typeof(CitizenInstance), typeof(Vector3), typeof(Vector3), typeof(VehicleInfo), typeof(bool), typeof(bool)],
           [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]

        public static bool StartPathFind(CitizenAI __instance, ushort instanceID, ref CitizenInstance citizenData, Vector3 startPos, Vector3 endPos, VehicleInfo vehicleInfo, bool enableTransport, bool ignoreCost, ref bool __result)
        {
            NetInfo.LaneType laneType = NetInfo.LaneType.Pedestrian;
            VehicleInfo.VehicleType vehicleType = VehicleInfo.VehicleType.None;
            VehicleInfo.VehicleCategory vehicleCategory = VehicleInfo.VehicleCategory.None;
            bool randomParking = false;
            bool combustionEngine = false;
            CitizenManager instance2 = Singleton<CitizenManager>.instance;
            if (vehicleInfo != null)
            {
                if (vehicleInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTaxi)
                {
                    if ((citizenData.m_flags & CitizenInstance.Flags.CannotUseTaxi) == 0 && Singleton<DistrictManager>.instance.m_districts.m_buffer[0].m_productionData.m_finalTaxiCapacity != 0)
                    {
                        SimulationManager instance = Singleton<SimulationManager>.instance;
                        if (instance.m_isNightTime || instance.m_randomizer.Int32(2u) == 0)
                        {
                            laneType |= NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle;
                            vehicleType |= vehicleInfo.m_vehicleType;
                            vehicleCategory |= vehicleInfo.vehicleCategory;
                        }
                    }
                }
                else
                {
                    if ((vehicleInfo.m_vehicleType == VehicleInfo.VehicleType.Car &&
                        (instance2.m_citizens.m_buffer[citizenData.m_citizen].m_vehicle != 0 || HumanAIPatch.IsRoadConnection(citizenData.m_sourceBuilding))) ||
                        vehicleInfo.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
                    {
                        laneType |= NetInfo.LaneType.Vehicle;
                        vehicleType |= vehicleInfo.m_vehicleType;
                        vehicleCategory |= vehicleInfo.vehicleCategory;
                    }
                    if (citizenData.m_targetBuilding != 0)
                    {
                        if ((citizenData.m_flags & CitizenInstance.Flags.TargetIsNode) != 0)
                        {
                            randomParking = true;
                        }
                        else if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].Info.m_class.m_service > ItemClass.Service.Office)
                        {
                            randomParking = true;
                        }
                        if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding].Info.GetAI() is CarRentalAI)
                        {
                            randomParking = false;
                        }
                    }
                    if (vehicleInfo.m_vehicleType == VehicleInfo.VehicleType.Car)
                    {
                        combustionEngine = vehicleInfo.m_class.m_subService == ItemClass.SubService.ResidentialLow;
                    }
                }
            }
            PathUnit.Position pathPos = default;
            ushort parkedVehicle = instance2.m_citizens.m_buffer[citizenData.m_citizen].m_parkedVehicle;
            if (parkedVehicle != 0)
            {
                Vector3 position = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[parkedVehicle].m_position;
                PathManager.FindPathPosition(position, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, VehicleInfo.VehicleType.Car, VehicleInfo.VehicleCategory.All, allowUnderground: false, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: true, out pathPos);
            }
            bool allowUnderground = (citizenData.m_flags & (CitizenInstance.Flags.Underground | CitizenInstance.Flags.Transition)) != 0;
            bool stablePath;
            float maxLength;
            if ((citizenData.m_flags & CitizenInstance.Flags.OnTour) != 0)
            {
                stablePath = true;
                maxLength = 160000f;
                laneType &= ~(NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle);
            }
            else
            {
                stablePath = false;
                maxLength = 20000f;
            }
            if (__instance.FindPathPosition(instanceID, ref citizenData, startPos, laneType, vehicleType, vehicleCategory, allowUnderground, out var position2) && __instance.FindPathPosition(instanceID, ref citizenData, endPos, laneType, vehicleType, vehicleCategory, allowUnderground: false, out var position3))
            {
                if (enableTransport && (citizenData.m_flags & CitizenInstance.Flags.CannotUseTransport) == 0)
                {
                    laneType |= NetInfo.LaneType.PublicTransport;
                    uint citizen = citizenData.m_citizen;
                    if (citizen != 0 && (instance2.m_citizens.m_buffer[citizen].m_flags & Citizen.Flags.Evacuating) != 0)
                    {
                        laneType |= NetInfo.LaneType.EvacuationTransport;
                    }
                }
                PathUnit.Position position4 = default;
                if (Singleton<PathManager>.instance.CreatePath(out var unit, ref Singleton<SimulationManager>.instance.m_randomizer, Singleton<SimulationManager>.instance.m_currentBuildIndex, position2, position4, position3, position4, pathPos, laneType, vehicleType, vehicleCategory, maxLength, isHeavyVehicle: false, ignoreBlocked: false, stablePath, skipQueue: false, randomParking, ignoreFlooded: false, combustionEngine, ignoreCost))
                {
                    if (citizenData.m_path != 0)
                    {
                        Singleton<PathManager>.instance.ReleasePath(citizenData.m_path);
                    }
                    citizenData.m_path = unit;
                    citizenData.m_flags |= CitizenInstance.Flags.WaitingPath;
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(CitizenAI), "FindPathPosition")]
        [HarmonyPrefix]
        public static void PatchFindPathPosition(ushort instanceID, ref CitizenInstance citizenData, Vector3 pos, ref NetInfo.LaneType laneTypes, ref VehicleInfo.VehicleType vehicleTypes, ref VehicleInfo.VehicleCategory vehicleCategories, bool allowUnderground, ref PathUnit.Position position)
        {
            var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
            var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
            if (citizenData.Info.GetAI() is TouristAI && targetBuilding.Info.GetAI() is CarRentalAI && VehicleRentalManager.VehicleRentalExist(citizenData.m_citizen))
            {
                laneTypes |= NetInfo.LaneType.Vehicle;
                vehicleTypes |= VehicleInfo.VehicleType.Car;
                vehicleCategories |= VehicleInfo.VehicleCategory.PassengerCar;
            }
            else if (citizenData.Info.GetAI() is ResidentAI && targetBuilding.Info.GetAI() is CarDealerAI && (citizen.m_vehicle != 0 || citizen.m_parkedVehicle != 0))
            {
                if(citizen.m_vehicle != 0 && Singleton<VehicleManager>.instance.m_vehicles.m_buffer[citizen.m_vehicle].Info.GetAI() is PassengerCarAI ||
                   citizen.m_parkedVehicle != 0 && Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[citizen.m_parkedVehicle].Info.GetAI() is PassengerCarAI)
                {
                    laneTypes |= NetInfo.LaneType.Vehicle;
                    vehicleTypes |= VehicleInfo.VehicleType.Car;
                    vehicleCategories |= VehicleInfo.VehicleCategory.PassengerCar;
                }
            }
        }
    }
}
