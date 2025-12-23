using System;
using UnityEngine;

namespace VehicleUnjam
{
    [Serializable]
    public struct ThemeData
    {
        [SerializeField] private GameObject _environmentPrefab;
        [SerializeField] private GameObject _waitingTilePrefab;
        [SerializeField] private PassengerSettingsSO _passengerSettings;
        [SerializeField] private GameObject _vehiclePrefab;
        [SerializeField] private CellSettingsSO _cellSettings;
        
        public GameObject environmentPrefab => _environmentPrefab;
        public GameObject waitingTilePrefab() => _waitingTilePrefab;
        public GameObject vehiclePrefab() => _vehiclePrefab;
        
        public GameObject GetCellPrefabByType(eCellType type)
        {
            return _cellSettings.GetPrefabByType(type);
        }
        
        public GameObject GetPassengerPrefabByType(ePassengerType type)
        {
            return _passengerSettings.GetPrefabByType(type);
        }
    }
    
    [Serializable]
    public enum eThemeType
    {
        Default = 0,
        FerryTerminal,
        TrainStation,
        Airport
    }
}