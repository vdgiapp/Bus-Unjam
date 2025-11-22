using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusUnjam
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
        public List<Passenger> passengers;
    }
    
    [Serializable]
    public class HiddenCellData : CellData
    {
        public bool isRevealed = false;
    }
    
    [Serializable]
    public class IcedCellData : CellData
    {
        public int iceHealth = 4;
    }
    
    [Serializable]
    public enum eCellType
    {
        None = -1, // For level designer tool
        Normal,
        Tunnel,
        Hidden,
        Iced
        // bla bla, ble ble
    }
}