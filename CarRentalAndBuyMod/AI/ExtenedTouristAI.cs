using MoreTransferReasons;

namespace CarRentalAndBuyMod.AI
{
    internal class ExtenedTouristAI : TouristAI, IExtendedCitizenAI
    {
        public void ExtendedStartTransfer(uint citizenID, ref Citizen data, ExtendedTransferManager.TransferReason material, ExtendedTransferManager.Offer offer)
        {
            if (data.m_flags == Citizen.Flags.None || data.Dead || data.Sick)
            {
                return;
            }
            switch (material)
            {
                case ExtendedTransferManager.TransferReason.CarRent:
                    data.m_flags &= ~Citizen.Flags.Evacuating;
                    ushort building = offer.Building;
                    var source_building = data.GetBuildingByLocation();
                    if (building != 0 && StartMoving(citizenID, ref data, source_building, building))
                    {
                        data.SetVisitplace(citizenID, offer.Building, 0u);
                    }
                    break;
            }
        }

    }
}
