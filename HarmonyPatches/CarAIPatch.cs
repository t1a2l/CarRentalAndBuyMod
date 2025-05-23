using CarRentalAndBuyMod.Managers;
using HarmonyLib;
using MoreTransferReasons;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    public static class CarAIPatch
    {
        [HarmonyPatch(typeof(CarAI), "PathfindFailure")]
        [HarmonyPostfix]
        public static void PathfindFailure(ushort vehicleID, ref Vehicle data)
        {
            if ((data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelVehicle || data.m_custom == (ushort)ExtendedTransferManager.TransferReason.FuelElectricVehicle)
               && VehicleFuelManager.FuelDataExist(vehicleID))
            {
                var fuelData = VehicleFuelManager.GetFuelData(vehicleID);
                var targetBuilding = fuelData.OriginalTargetBuilding;

                if (data.Info.m_vehicleAI is CargoTruckAI cargoTruckAI && (data.m_targetBuilding != 0))
                {
                    cargoTruckAI.SetTarget(vehicleID, ref data, targetBuilding);
                }
                else if (data.Info.m_vehicleAI is PassengerCarAI passengerCarAI && (data.m_targetBuilding != 0))
                {
                    passengerCarAI.SetTarget(vehicleID, ref data, targetBuilding);
                }
            }
        }
    }
}
