using CarRentalAndBuyMod.Managers;
using System;
using UnityEngine;

namespace CarRentalAndBuyMod.Serializer
{
    public class VehicleFuelManagerSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iVEHICLE_FUEL_MANAGER_DATA_VERSION = 2;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iVEHICLE_FUEL_MANAGER_DATA_VERSION, Data);
            StorageData.WriteInt32(VehicleFuelManager.VehiclesFuel.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in VehicleFuelManager.VehiclesFuel)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);
                StorageData.WriteFloat(kvp.Value.CurrentFuelCapacity, Data);
                StorageData.WriteFloat(kvp.Value.MaxFuelCapacity, Data);
                StorageData.WriteUInt16(kvp.Value.OriginalTargetBuilding, Data);
                StorageData.WriteBool(kvp.Value.IsParked, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iVehicleFuelManagerVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("CarRentalAndBuyMod - Global: " + iGlobalVersion + " BufferVersion: " + iVehicleFuelManagerVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                VehicleFuelManager.VehiclesFuel ??= [];
                int VehiclesFuel_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < VehiclesFuel_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iVehicleFuelManagerVersion, Data, ref iIndex);

                    ushort vehicleId = StorageData.ReadUInt16(Data, ref iIndex);

                    float currentFuelCapacity = StorageData.ReadFloat(Data, ref iIndex);

                    float maxFuelCapacity = StorageData.ReadFloat(Data, ref iIndex);

                    ushort originalTargetBuilding = StorageData.ReadUInt16(Data, ref iIndex);

                    if (!VehicleFuelManager.VehiclesFuel.TryGetValue(vehicleId, out _))
                    {
                        var vehicleFuelCapacity = new VehicleFuelManager.VehicleFuelCapacity
                        {
                            CurrentFuelCapacity = currentFuelCapacity,
                            MaxFuelCapacity = maxFuelCapacity,
                            OriginalTargetBuilding = originalTargetBuilding
                        };

                        if(iVehicleFuelManagerVersion == 1)
                        {
                            vehicleFuelCapacity.IsParked = false;
                        }
                        else
                        {
                            bool isParked = StorageData.ReadBool(Data, ref iIndex);
                            vehicleFuelCapacity.IsParked = isParked;
                        }

                        VehicleFuelManager.VehiclesFuel.Add(vehicleId, vehicleFuelCapacity);
                    }

                    CheckEndTuple($"Buffer({i})", iVehicleFuelManagerVersion, Data, ref iIndex);
                }

                if(iVehicleFuelManagerVersion == 1)
                {
                    int ParkedVehiclesFuel_Count = StorageData.ReadInt32(Data, ref iIndex);
                    for (int i = 0; i < ParkedVehiclesFuel_Count; i++)
                    {
                        CheckStartTuple($"Buffer({i})", iVehicleFuelManagerVersion, Data, ref iIndex);

                        ushort vehicleId = StorageData.ReadUInt16(Data, ref iIndex);

                        float currentFuelCapacity = StorageData.ReadFloat(Data, ref iIndex);

                        float maxFuelCapacity = StorageData.ReadFloat(Data, ref iIndex);

                        ushort originalTargetBuilding = StorageData.ReadUInt16(Data, ref iIndex);

                        if (!VehicleFuelManager.VehiclesFuel.TryGetValue(vehicleId, out _))
                        {
                            var vehicleFuelCapacity = new VehicleFuelManager.VehicleFuelCapacity
                            {
                                CurrentFuelCapacity = currentFuelCapacity,
                                MaxFuelCapacity = maxFuelCapacity,
                                OriginalTargetBuilding = originalTargetBuilding,
                                IsParked = true
                            };

                            VehicleFuelManager.VehiclesFuel.Add(vehicleId, vehicleFuelCapacity);
                        }

                        CheckEndTuple($"Buffer({i})", iVehicleFuelManagerVersion, Data, ref iIndex);
                    }
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
                    throw new Exception($"VehiclesFuel Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"VehiclesFuel Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
