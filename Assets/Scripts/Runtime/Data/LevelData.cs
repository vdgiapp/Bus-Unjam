using System;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleUnjam
{
    [Serializable]
    public class LevelData
    {
        public int rows = 0;
        public int columns = 0;
        public int waitAreaSize = 0;
        
        public List<CellData> cells = new();
        public List<PassengerData> passengers = new();
        public List<VehicleData> vehicles = new();

        // Helper methods
        public int Index(int r, int c)
        {
            return ((r * columns) + c);
        }
        
        public CellData GetCellData(int row, int col)
        {
            if (!IsValidPosition(row, col)) return null;
            int index = Index(row, col);
            return index < cells.Count ? cells[index] : null;
        }

        public PassengerData GetPassengerData(int row, int col)
        {
            if (!IsValidPosition(row, col)) return null;
            int index = Index(row, col);
            return index < passengers.Count ? passengers[index] : null;
        }
        
        public VehicleData GetVehicleData(int vehicleIndex)
        {
            return vehicleIndex >= 0 && vehicleIndex < vehicles.Count 
                ? vehicles[vehicleIndex] 
                : null;
        }
        
        public bool IsValidPosition(int r, int c)
        {
            return r >= 0 && r < rows && c >= 0 && c < columns;
        }
        
        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < (rows * columns);
        }
        
        public (int row, int col) GetPosition(int index)
        {
            if (!IsValidIndex(index)) return (-1, -1);
            return (index / columns, index % columns);
        }
    }
}