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
                    if (StartMoving(citizenID, ref data, data.m_visitBuilding, offer.Building))
                    {
                        data.SetVisitplace(citizenID, offer.Building, 0u);
                    }
                    break;
            }
        }

    }
}
