using System;
using Sirenix.OdinSerializer;
using UnityEngine;

namespace VehicleUnjam
{
    [Serializable]
    public enum ePassengerType
    {
        Normal = 0,
        Rope,
        Hidden,
        Bomb,
        Cloak
    }
    
    // File level sẽ chỉ ghi class PassengerData này
    // Khi đọc thì sẽ dựa vào extraDataJson để tạo class và đặt data cho Passenger
    [Serializable]
    public class PassengerData
    {
        public eColorType colorType = eColorType.Red;
        public ePassengerType passengerType = ePassengerType.Normal;
        
        [OdinSerialize]
        public PassengerExtraData extraData;
    }
    
    [Serializable]
    public abstract class PassengerExtraData {}
    
    [Serializable]
    public class RopePassengerData : PassengerExtraData
    {
        public int ropeCount = 0;
    }
    
    [Serializable]
    public class BombPassengerData : PassengerExtraData
    {
        public int bombTime = 0;
    }
    
    [Serializable]
    public class CloakPassengerData : PassengerExtraData
    {
        public bool isRevealed = false;
    }
}