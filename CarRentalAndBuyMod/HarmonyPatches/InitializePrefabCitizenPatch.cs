using MoreTransferReasons.AI;
using CarRentalAndBuyMod.Utils;
using HarmonyLib;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch(typeof(CitizenInfo), "InitializePrefab")]
    public static class InitializePrefabCitizenPatch
    {
        public static void Prefix(CitizenInfo __instance)
        {
            var oldAI = __instance.GetComponent<PrefabAI>();
            if (oldAI != null && oldAI is TouristAI)
            {
                Object.DestroyImmediate(oldAI);
                var newAI = (PrefabAI)__instance.gameObject.AddComponent<ExtenedTouristAI>();
                PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
            }
            else if (oldAI != null && oldAI is ResidentAI)
            {
                Object.DestroyImmediate(oldAI);
                var newAI = (PrefabAI)__instance.gameObject.AddComponent<ExtenedResidentAI>();
                PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
            }
        }
    }
}
