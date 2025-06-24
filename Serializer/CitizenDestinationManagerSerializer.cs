using System;
using CarRentalAndBuyMod.Managers;
using UnityEngine;

namespace CarRentalAndBuyMod.Serializer
{
    public class CitizenDestinationManagerSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iCITIZEN_DESTINATION_MANAGER_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iCITIZEN_DESTINATION_MANAGER_DATA_VERSION, Data);
            StorageData.WriteInt32(CitizenDestinationManager.CitizenDestination.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in CitizenDestinationManager.CitizenDestination)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt32(kvp.Key, Data);
                StorageData.WriteUInt16(kvp.Value, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iCitizenDestinationManagerVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("CarRentalAndBuyMod CitizenDestination - Global: " + iGlobalVersion + " BufferVersion: " + iCitizenDestinationManagerVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                CitizenDestinationManager.CitizenDestination ??= [];
                int CitizenDestination_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < CitizenDestination_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iCitizenDestinationManagerVersion, Data, ref iIndex);

                    uint citizenId = StorageData.ReadUInt32(Data, ref iIndex);

                    ushort buildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    CitizenDestinationManager.CitizenDestination.Add(citizenId, buildingId);

                    CheckEndTuple($"Buffer({i})", iCitizenDestinationManagerVersion, Data, ref iIndex);
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
                    throw new Exception($"CitizenDestination Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"CitizenDestination Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
