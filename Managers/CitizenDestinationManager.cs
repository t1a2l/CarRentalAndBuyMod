using System.Collections.Generic;

namespace CarRentalAndBuyMod.Managers
{
    public static class CitizenDestinationManager
    {
        public static Dictionary<uint, ushort> CitizenDestination;

        public static void Init()
        {
            CitizenDestination ??= [];
        }

        public static void Deinit() => CitizenDestination = [];

        public static ushort GetCitizenDestination(uint citizenId) => CitizenDestination.TryGetValue(citizenId, out var buildingId) ? buildingId : default;

        public static ushort CreateCitizenDestination(uint citizenId, ushort buildingId)
        {
            CitizenDestination.Add(citizenId, buildingId);

            return buildingId;
        }

        public static bool CitizenDestinationExist(uint citizenId) => CitizenDestination.ContainsKey(citizenId);

        public static void SetCitizenDestination(uint citizenId, ushort buildingId)
        {
            if (CitizenDestination.TryGetValue(citizenId, out var _))
            {
                CitizenDestination[citizenId] = buildingId;
            }
        }


        public static void RemoveCitizenDestination(uint citizenId)
        {
            if (CitizenDestination.TryGetValue(citizenId, out var _))
            {
                CitizenDestination.Remove(citizenId);
            }
        }
    }

}
