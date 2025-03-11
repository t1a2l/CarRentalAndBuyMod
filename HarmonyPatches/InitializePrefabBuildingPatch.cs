using System;
using HarmonyLib;
using Object = UnityEngine.Object;
using CarRentalAndBuyMod.AI;
using CarRentalAndBuyMod.Utils;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch(typeof(BuildingInfo), "InitializePrefab")]
    public static class InitializePrefabBuildingPatch
    {
        public static void Prefix(BuildingInfo __instance)
        {
            try
            {
                if ((__instance.name.ToLower().Contains("gas station") || __instance.name.ToLower().Contains("gasstation") || __instance.name.ToLower().Contains("gaspumps")) && __instance.GetAI() is not GasStationAI)
                {
                    var oldAI = __instance.GetComponent<PrefabAI>();
                    Object.DestroyImmediate(oldAI);
                    var newAI = (PrefabAI)__instance.gameObject.AddComponent<GasStationAI>();
                    PrefabUtil.TryCopyAttributes(oldAI, newAI, false);

                    __instance.m_placementStyle = ItemClass.Placement.Manual;
                }
                else if ((__instance.name.ToLower().Contains("hertz") || __instance.name.ToLower().Contains("enterprise")) && __instance.GetAI() is not CarRentalAI)
                {
                    var oldAI = __instance.GetComponent<PrefabAI>();
                    Object.DestroyImmediate(oldAI);
                    var newAI = (PrefabAI)__instance.gameObject.AddComponent<CarRentalAI>();
                    PrefabUtil.TryCopyAttributes(oldAI, newAI, false);

                    __instance.m_placementStyle = ItemClass.Placement.Manual;
                }
                else if (__instance.name.ToLower().Contains("dealership") && __instance.GetAI() is not CarDealerAI)
                {
                    var oldAI = __instance.GetComponent<PrefabAI>();
                    Object.DestroyImmediate(oldAI);
                    var newAI = (PrefabAI)__instance.gameObject.AddComponent<CarDealerAI>();
                    PrefabUtil.TryCopyAttributes(oldAI, newAI, false);

                    __instance.m_placementStyle = ItemClass.Placement.Manual;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

    }
}