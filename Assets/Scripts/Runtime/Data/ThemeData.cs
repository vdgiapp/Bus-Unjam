using System;
using UnityEngine;

namespace BusUnjam
{
    [Serializable]
    public class ThemeData
    {
        [SerializeField] private GameObject _environmentPrefab;
        [SerializeField] private GameObject _waitingTilePrefab;
        [SerializeField] private GameObject _vehiclePrefab;
        [SerializeField] private CellSettingsSO _cellSettings;
        [SerializeField] private PassengerSettingsSO _passengerSettings;
        
        public GameObject GetEnvironmentPrefab() => _environmentPrefab;
        
        public GameObject GetWaitingTilePrefab() => _waitingTilePrefab;

        public GameObject GetVehiclePrefab() => _vehiclePrefab;
        
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