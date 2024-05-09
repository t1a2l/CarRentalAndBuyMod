using System;
using ICities;
using UnityEngine;

namespace CarRentalAndBuyMod.Serializer
{
    public class CarRentalAndBuyModSerializer : ISerializableDataExtension
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        public const ushort DataVersion = 1;
        public const string DataID = "CarRentalAndBuyMod";

        public static CarRentalAndBuyModSerializer instance = null;
        private ISerializableData m_serializableData = null;

        public void OnCreated(ISerializableData serializedData)
        {
            instance = this;
            m_serializableData = serializedData;
        }

        public void OnLoadData()
        {
            try
            {
                if (m_serializableData != null)
                {
                    byte[] Data = m_serializableData.LoadData(DataID);
                    if (Data != null && Data.Length > 0)
                    {
                        ushort SaveGameFileVersion;
                        int Index = 0;

                        SaveGameFileVersion = StorageData.ReadUInt16(Data, ref Index);

                        Debug.Log("Data length: " + Data.Length.ToString() + "; Data Version: " + SaveGameFileVersion);

                        if (SaveGameFileVersion <= DataVersion)
                        {
                            while (Index < Data.Length)
                            {
                                CheckStartTuple("CitizenDestinationManagerSerializer", SaveGameFileVersion, Data, ref Index);
                                CitizenDestinationManagerSerializer.LoadData(SaveGameFileVersion, Data, ref Index);
                                CheckEndTuple("CitizenDestinationManagerSerializer", SaveGameFileVersion, Data, ref Index);

                                if (Index == Data.Length)
                                {
                                    break;
                                }

                                CheckStartTuple("VehicleRentalManagerSerializer", SaveGameFileVersion, Data, ref Index);
                                VehicleRentalManagerSerializer.LoadData(SaveGameFileVersion, Data, ref Index);
                                CheckEndTuple("VehicleRentalManagerSerializer", SaveGameFileVersion, Data, ref Index);

                                if (Index == Data.Length)
                                {
                                    break;
                                }

                                CheckStartTuple("GasStationFuelManagerSerializer", SaveGameFileVersion, Data, ref Index);
                                GasStationFuelManagerSerializer.LoadData(SaveGameFileVersion, Data, ref Index);
                                CheckEndTuple("GasStationFuelManagerSerializer", SaveGameFileVersion, Data, ref Index);

                                if (Index == Data.Length)
                                {
                                    break;
                                }

                                CheckStartTuple("VehicleFuelManagerSerializer", SaveGameFileVersion, Data, ref Index);
                                VehicleFuelManagerSerializer.LoadData(SaveGameFileVersion, Data, ref Index);
                                CheckEndTuple("VehicleFuelManagerSerializer", SaveGameFileVersion, Data, ref Index);
                                break;
                            }
                        }
                        else
                        {
                            string sMessage = "This saved game was saved with a newer version of CarRentalAndBuyMod.\r\n";
                            sMessage += "\r\n";
                            sMessage += "Unable to load settings.\r\n";
                            sMessage += "\r\n";
                            sMessage += "Saved game data version: " + SaveGameFileVersion + "\r\n";
                            sMessage += "MOD data version: " + DataVersion + "\r\n";
                            Debug.Log(sMessage);
                        }
                    }
                    else
                    {
                        Debug.Log("Data is null");
                    }
                }
                else
                {
                    Debug.Log("m_serializableData is null");
                }
            }
            catch (Exception ex)
            {
                string sErrorMessage = "Loading of CarRentalAndBuyMod save game settings failed with the following error:\r\n";
                sErrorMessage += "\r\n";
                sErrorMessage += ex.Message;
                Debug.LogError(sErrorMessage);
            }
        }

        public void OnSaveData()
        {
            Debug.Log("OnSaveData - Start");
            try
            {
                if (m_serializableData != null)
                {
                    var Data = new FastList<byte>();
                    // Always write out data version first
                    StorageData.WriteUInt16(DataVersion, Data);

                    // car rental citizen original destination
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    CitizenDestinationManagerSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // rented cars
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    VehicleRentalManagerSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // Gas Stations Fuel settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    GasStationFuelManagerSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // Vehicles Fuel settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    VehicleFuelManagerSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    m_serializableData.SaveData(DataID, Data.ToArray());
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Could not save data. " + ex.Message);
            }
            Debug.Log("OnSaveData - Finish");
        }

        private void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"CarRentalAndBuyMod Start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private void CheckEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"CarRentalAndBuyMod End tuple not found at: {sTupleLocation}");
                }
            }
        }

        public void OnReleased() => instance = null;

    }
}
