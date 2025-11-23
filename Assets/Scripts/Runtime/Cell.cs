using System;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleUnjam
{
    public class Cell : MonoBehaviour
    {
        [HideInInspector] public CellData data;
        
        [SerializeField] private Animator _animator;
    }
    
    [Serializable]
    public class CellData
    {
        public eCellType cellType;
        public bool isOccupied;
    }
    
    [Serializable]
    public class TunnelCellData : CellData
    {
        public readonly List<Passenger> passengers = new();
    }
    
    
    [Serializable]
    public enum eCellType
    {
        None = -1, // For level designer tool
        Normal,
        Tunnel
        // bla bla, ble ble
    }
}