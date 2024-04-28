using System;
using CarRentalAndBuyMod.Utils;
using ColossalFramework;
using UnityEngine;

namespace CarRentalAndBuyMod.Serializer
{
    public class VehicleRentalManagerSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iVEHICLE_RENTAL_MANAGER_DATA_VERSION = 2;

        public static void SaveData(FastList<byte> Data)
        {
            Debug.Log("save VehicleRentals_Count: " + VehicleRentalManager.VehicleRentals.Count);

            // Write out metadata
            StorageData.WriteUInt16(iVEHICLE_RENTAL_MANAGER_DATA_VERSION, Data);
            StorageData.WriteInt32(VehicleRentalManager.VehicleRentals.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in VehicleRentalManager.VehicleRentals)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt32(kvp.Key, Data);
                StorageData.WriteUInt16(kvp.Value.RentedVehicleID, Data);
                StorageData.WriteUInt16(kvp.Value.CarRentalBuildingID, Data);
                StorageData.WriteBool(kvp.Value.IsRemovedToSpawn, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iVehicleRentalManagerVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("Global: " + iGlobalVersion + " BufferVersion: " + iVehicleRentalManagerVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                if (VehicleRentalManager.VehicleRentals == null)
                {
                    VehicleRentalManager.VehicleRentals = [];
                }
                int VehicleRentals_Count = StorageData.ReadInt32(Data, ref iIndex);
                Debug.Log("load VehicleRentals_Count: " + VehicleRentals_Count);
                for (int i = 0; i < VehicleRentals_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", VehicleRentals_Count, Data, ref iIndex);

                    uint citizenId = StorageData.ReadUInt32(Data, ref iIndex);
                    ushort rentedVehicleID = StorageData.ReadUInt16(Data, ref iIndex);
                    ushort carRentalBuildingID = StorageData.ReadUInt16(Data, ref iIndex);

                    var rental = new VehicleRentalManager.Rental()
                    {
                        RentedVehicleID = rentedVehicleID,
                        CarRentalBuildingID = carRentalBuildingID
                    };

                    if (iVehicleRentalManagerVersion == 2)
                    {
                        bool isRemovedToSpawned = StorageData.ReadBool(Data, ref iIndex);
                        rental.IsRemovedToSpawn = isRemovedToSpawned;
                    }
                    else
                    {
                        var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[rental.RentedVehicleID];
                        if (vehicle.Info.GetAI() is PassengerCarAI)
                        {
                            rental.IsRemovedToSpawn = false;
                        }
                        else
                        {
                            rental.IsRemovedToSpawn = true;
                        }
                    }

                    VehicleRentalManager.VehicleRentals.Add(citizenId, rental);

                    CheckEndTuple($"Buffer({i})", iVehicleRentalManagerVersion, Data, ref iIndex);
                }
            }
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"VehicleRental Buffer start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private static void CheckEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"VehicleRental Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
