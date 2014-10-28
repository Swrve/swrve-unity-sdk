using System;

namespace Swrve.Device
{
    public interface ICarrierInfo
    {
        string GetName();
        string GetIsoCountryCode();
        string GetCarrierCode();
    }
}

