using System.Collections.Generic;

namespace CarRentalAndBuyMod.Utils
{
    public static class CitizenDestinationManager
    {
        public static Dictionary<uint, ushort> CitizenDestination;

        public static void Init()
        {
            CitizenDestination ??= [];
        }

        public static void Deinit() => CitizenDestination = [];

        public static ushort GetCitizenDestination(uint citizenId) => !CitizenDestination.TryGetValue(citizenId, out var buildingId) ? default : buildingId;

        public static void CreateCitizenDestination(uint citizenId, ushort buildingId)
        {
            if (!CitizenDestination.TryGetValue(citizenId, out _))
            {
                CitizenDestination.Add(citizenId, buildingId);
            }
        }

        public static void SetCitizenDestination(uint citizenId, ushort buildingId) => CitizenDestination[citizenId] = buildingId;


        public static void RemoveCitizenDestination(uint citizenId) => CitizenDestination.Remove(citizenId);
    }

}
