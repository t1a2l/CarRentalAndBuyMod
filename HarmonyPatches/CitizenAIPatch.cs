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
        [HarmonyBefore(["me.tmpe"])]
        [HarmonyPatch(typeof(CitizenAI), "StartPathFind",
            [typeof(ushort), typeof(CitizenInstance), typeof(Vector3), typeof(Vector3), typeof(VehicleInfo), typeof(bool), typeof(bool)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void StartPathFindPrefix(CitizenAI __instance, ushort instanceID, ref CitizenInstance citizenData, ref VehicleInfo vehicleInfo)
        {
            if (__instance is TouristAI)
            {
                var targetBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenData.m_targetBuilding];
                var rental = VehicleRentalManager.GetVehicleRental(citizenData.m_citizen);
                if (targetBuilding.Info.GetAI() is CarRentalAI && !VehicleRentalManager.VehicleRentalExist(citizenData.m_citizen))
                {
                    Debug.Log("vehicleInfoNull");
                    vehicleInfo = null;
                }
            }
        }
    }
}
