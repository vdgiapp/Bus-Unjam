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
    public struct CellData
    {
        public eCellType cellType;
        public bool isOccupied;
    }
    
    [Serializable]
    public enum eCellType
    {
        None = -1,
        Normal,
        // bla bla, ble ble
    }
}