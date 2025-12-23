using System;
using System.Collections.Generic;
using Sirenix.OdinSerializer;
using UnityEngine;

namespace VehicleUnjam
{
    [Serializable]
    public enum eCellType
    {
        None = 0,
        Normal,
        Tunnel,
    }

    // File level sẽ chỉ ghi class CellData này
    // Khi đọc thì sẽ dựa vào extraDataJson để tạo class và đặt data cho Cell
    [Serializable]
    public class CellData
    {
        public eCellType cellType = eCellType.None;
        public bool isOccupied = false;
        
        [OdinSerialize]
        public CellExtraData extraData;
    }
    
    [Serializable]
    public abstract class CellExtraData {}

    [Serializable]
    public class TunnelCellData : CellExtraData
    {
        public int direction;
        public List<PassengerData> passengers = new();
    }
}