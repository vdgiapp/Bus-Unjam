using System;
using UnityEngine;

namespace BusUnjam
{
    [Serializable]
    public class LevelData
    {
        public int rows;
        public int columns;
        public int waitAreaSize;
        
        [SerializeReference] public CellData[] cells;
        [SerializeReference] public PassengerData[] passengers;
        [SerializeReference] public VehicleData[] vehicles;

        // Helper methods
        public int Index(int r, int c) => r * columns + c;

        public CellData GetCellData(int r, int c)
        {
            CellData ret = cells[Index(r, c)];
            if (ret != null) return ret;
            return null;
        }

        public PassengerData GetPassengerData(int r, int c)
        {
            PassengerData ret = passengers[Index(r, c)];
            if (ret != null) return ret;
            return null;
        }
    }
}