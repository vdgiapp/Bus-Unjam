using System;
using UnityEngine;

namespace VehicleUnjam
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
        public int Index(int r, int c)
        {
            return ((r * columns) + c);
        }
        
        public CellData GetCellData(int r, int c)
        {
            return cells[Index(r, c)];
        }

        public PassengerData GetPassengerData(int r, int c)
        {
            return passengers[Index(r, c)];
        }
    }
}