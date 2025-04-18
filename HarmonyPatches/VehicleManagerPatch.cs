using CarRentalAndBuyMod.Managers;
using ColossalFramework.Math;
using ColossalFramework;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace CarRentalAndBuyMod.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleManagerPatch
    {
        [HarmonyPatch(typeof(VehicleManager), "ReleaseVehicle")]
        [HarmonyPrefix]
        public static void ReleaseVehicle(ushort vehicle)
        {
            VehicleRemoved(vehicle);
        }

        [HarmonyPatch(typeof(VehicleManager), "ReleaseParkedVehicle")]
        [HarmonyPrefix]
        public static void ReleaseParkedVehicle(ushort parked)
        {
            VehicleRemoved(parked);
        }

        public static VehicleInfo GetVehicleInfo(ref CitizenInstance citizenData)
        {
            Citizen.Wealth wealthLevel = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen].WealthLevel;
            int camper_probability = GetCamperProbability(wealthLevel);
            int car_probability = GetCarProbability(citizenData.m_frame1.m_position);
            int electricCarProbability = GetElectricCarProbability(wealthLevel);
            ItemClass.Service service = ItemClass.Service.Residential;
            ItemClass.SubService subService = ItemClass.SubService.ResidentialLow;
            ItemClass.Level level = ItemClass.Level.Level1;
            Randomizer r = new(citizenData.m_citizen);
            if (car_probability >= camper_probability)
            {
                if (car_probability >= electricCarProbability)
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHigh;
                        level = ItemClass.Level.Level2;
                    }
                }
                else
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHighEco;
                        level = ItemClass.Level.Level2;
                    }
                    else
                    {
                        subService = ItemClass.SubService.ResidentialLowEco;
                        level = ItemClass.Level.Level1;
                    }
                }
            }
            else
            {
                if (camper_probability >= electricCarProbability)
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHigh;
                        level = ItemClass.Level.Level2;
                    }
                }
                else
                {
                    int res = r.Int32(1);
                    if (res == 1)
                    {
                        subService = ItemClass.SubService.ResidentialHighEco;
                        level = ItemClass.Level.Level2;
                    }
                    else
                    {
                        subService = ItemClass.SubService.ResidentialLowEco;
                        level = ItemClass.Level.Level1;
                    }
                }
            }
            return Singleton<VehicleManager>.instance.GetRandomVehicleInfo(ref r, service, subService, level);
        }

        private static void VehicleRemoved(ushort VehicleId)
        {
            var rentalKey = VehicleRentalManager.VehicleRentals.FirstOrDefault(z => z.Value.RentedVehicleID == VehicleId).Key;

            if (VehicleRentalManager.RentalDataExist(rentalKey))
            {
                VehicleRentalManager.RemoveRentalData(rentalKey);
            }

            if (VehicleFuelManager.FuelDataExist(VehicleId))
            {
                VehicleFuelManager.RemoveFuelData(VehicleId);
            }
        }


        private static int GetCarProbability(Vector3 position)
        {
            byte park = Singleton<DistrictManager>.instance.GetPark(position);
            if (park != 0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[park].IsAirport && (Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_parkPolicies & DistrictPolicies.Park.CarRentals) != 0)
            {
                return 90;
            }
            return 20;
        }

        private static int GetCamperProbability(Citizen.Wealth wealth)
        {
            return wealth switch
            {
                Citizen.Wealth.Low => 20,
                Citizen.Wealth.Medium => 30,
                Citizen.Wealth.High => 40,
                _ => 0,
            };
        }

        private static int GetElectricCarProbability(Citizen.Wealth wealth)
        {
            return wealth switch
            {
                Citizen.Wealth.Low => 10,
                Citizen.Wealth.Medium => 15,
                Citizen.Wealth.High => 20,
                _ => 0,
            };
        }
    }
}
