using System;

namespace VehicleUnjam
{
    [Serializable]
    public class VehicleData
    {
        public eColorType colorType = eColorType.Red;
        public bool[] occupied = { false, false, false };
    }
}