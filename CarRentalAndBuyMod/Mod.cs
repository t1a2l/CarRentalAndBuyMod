using CitiesHarmony.API;
using ICities;
using CarRentalAndBuyMod.Utils;

namespace CarRentalAndBuyMod
{
    public class Mod : LoadingExtensionBase, IUserMod 
    {
        string IUserMod.Name => "Car Rental And Buy Mod";
        string IUserMod.Description => "Allow citizens to buy and sell vehicles to and from dealerships. allow tourist to rent vehicles while staying in the city";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => PatchUtil.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) PatchUtil.UnpatchAll();
        }

    }
}
