using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using System.Linq;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleWorldInfoPanelPatch
    {

        //[HarmonyPatch(typeof(VehicleWorldInfoPanel), "UpdateBindings")]
        //[HarmonyPostfix]
        //public static void UpdateBindings(VehicleWorldInfoPanel __instance,  ref UIButton ___Owner, ref InstanceID ___m_InstanceID)
        //{
        //    InstanceID target = default;
        //    var vehicleId = ___m_InstanceID.Vehicle;
        //    var rental = VehicleRentalManager.VehicleRentals.Where(z => z.Value.RentedVehicleID == vehicleId).GetEnumerator().Current.Value;
        //    if(!rental.Equals(default(VehicleRentalManager.Rental)))
        //    {
        //        target.Building = rental.CarRentalBuildingID;
        //        ___Owner.objectUserData = target;
        //        ___Owner.text = Singleton<BuildingManager>.instance.GetBuildingName(rental.CarRentalBuildingID, ___m_InstanceID);
        //        ShortenTextToFitParent(___Owner);
        //        ___Owner.isVisible = true;
        //        ___Owner.isEnabled = true;
        //    }
       
        //}

        public static void ShortenTextToFitParent(UIButton button)
        {
            float num = button.parent.width - button.relativePosition.x;
            if (button.width > num)
            {
                button.tooltip = button.text;
                string text = button.text;
                while (button.width > num && text.Length > 5)
                {
                    text = text.Substring(0, text.Length - 4);
                    text = text.Trim();
                    text = (button.text = text + "...");
                }
            }
            else
            {
                button.tooltip = string.Empty;
            }
        }

    }
}
