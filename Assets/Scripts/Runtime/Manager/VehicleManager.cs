using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class VehicleManager : MonoBehaviour
    {
        public static VehicleManager instance { get; private set; }
        
        public static event Action<int> VehicleIndexArrived;
        public static event Action LastVehicleLeft; // level complete

        [SerializeField] private Transform _vehicleContainer;
        
        private readonly List<Vehicle> _vehicleList = new();
        private int _currentVehicleIndex = -1;
        private bool _isVehiclesMoving = false;
        private LevelData _loadedLevelData;
        private Sequence _vehicleSequence;
        
        private bool _isInstanceInit = false;
        private bool _isWaitForInstanceInit = false;
        
        private void Awake()
        {
            instance = this;
            _isInstanceInit = true;
        }

        public async UniTask LoadVehicleFromLevelAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            if (!await WaitForInstanceInit()) return;

            _loadedLevelData = levelData;
            
            GameObject prefab = GetVehiclePrefab();
            GameObject[] loaded = await InstantiateAsync(prefab, _loadedLevelData.vehicles.Count, _vehicleContainer).ToUniTask();
            
            for (int i = 0; i < loaded.Length; i++)
            {
                Vehicle vehicle = loaded[i].GetComponent<Vehicle>();
                VehicleData data = _loadedLevelData.vehicles[i];
                
                vehicle.name = $"{Constants.VEHICLE_GAMEOBJECT_NAME} ({i})";
                vehicle.InitData(data);
                vehicle.SetColor(GameManager.GetColorByType(data.colorType));

                for (int u = 0; u < Constants.VEHICLE_SEAT_SLOTS; u++)
                {
                    vehicle.SetSeatOccupied(u, false);
                    vehicle.SetReservedPassenger(u, null);
                }
                
                // Position vehicle
                float xPosition = -1f * i * Constants.VEHICLE_DISTANCE;
                vehicle.transform.localPosition = new Vector3(xPosition, 0f, 0f);
                
                _vehicleList.Add(vehicle);
            }
            _currentVehicleIndex = 0;
            UpdateVehiclesActiveState();
        }
        
        private async UniTask<bool> WaitForInstanceInit()
        {
            if (_isWaitForInstanceInit) return false;
            if (_isInstanceInit) return true;
            _isWaitForInstanceInit = true;
            await UniTask.WaitUntil(() => _isInstanceInit);
            _isWaitForInstanceInit = false;
            return true;
        }

        private void UpdateVehiclesActiveState()
        {
            int start = _currentVehicleIndex;
            int end = _currentVehicleIndex + Constants.VEHICLE_ACTIVE_SIZE;
            for (int i = 0; i < _vehicleList.Count; i++)
            {
                bool active = (i >= start && i < end);
                _vehicleList[i].gameObject.SetActive(active);
            }
        }
        
        public Sequence NextVehicleSequence()
        {
            if (_isVehiclesMoving) return null;
            
            // Set flag
            _isVehiclesMoving = true;
            
            // Vehicle movement sequence
            _vehicleSequence?.Kill();
            _vehicleSequence = DOTween.Sequence();
            
            int start = _currentVehicleIndex;
            int end = Mathf.Min(_currentVehicleIndex + Constants.VEHICLE_ACTIVE_SIZE, _vehicleList.Count);
            
            _vehicleSequence.onComplete += () =>
            {
                _isVehiclesMoving = false;
                if (start < _vehicleList.Count) _vehicleList[start].gameObject.SetActive(false);
                _currentVehicleIndex++;
                UpdateVehiclesActiveState();
                VehicleIndexArrived?.Invoke(_currentVehicleIndex);
                if (_currentVehicleIndex >= _vehicleList.Count) LastVehicleLeft?.Invoke();
            };
            
            for (int i = start; i < end; i++)
            {
                Vehicle vehicle = _vehicleList[i];
                Vector3 targetPos = vehicle.transform.localPosition;

                if (i == start)
                {
                    // Move first vehicle out of screen (right)
                    targetPos.x = 2.5f * Constants.VEHICLE_DISTANCE;
                    _vehicleSequence.Join(vehicle.MoveLocalTo(targetPos, Constants.VEHICLE_MOVE_DURATION));
                }
                else
                {
                    // Shift vehicles forward
                    targetPos.x = -1f * (i - start - 1) * Constants.VEHICLE_DISTANCE;
                    _vehicleSequence.Join(vehicle.MoveLocalTo(targetPos, Constants.VEHICLE_MOVE_DURATION, Ease.OutQuad));
                }
            }
            return _vehicleSequence;
        }
        
        private bool IsValidLevelData(LevelData levelData)
        {
            return levelData.vehicles is { Count: > 0 };
        }
        
        private GameObject GetVehiclePrefab()
        {
            return GameManager.GetCurrentTheme().vehiclePrefab;
        }
        
        public bool IsVehicleFull(Vehicle vehicle)
        {
            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                if (!vehicle.seatOccupied[i] || !vehicle.reservedPassengers[i]) return false;
            }
            return true;
        }
        
        public int GetVehicleCount() => _vehicleList.Count;
        public int GetCurrentVehicleIndex() => _currentVehicleIndex;
        public bool IsVehiclesMoving() => _isVehiclesMoving;
        
        public Vehicle GetVehicleAtIndex(int index)
        {
            if (index < 0 || index >= _vehicleList.Count) return null;
            return _vehicleList[index];
        }

        public Vehicle GetCurrentVehicle()
        {
            if (_currentVehicleIndex < 0 || _currentVehicleIndex >= _vehicleList.Count) return null;
            return _vehicleList[_currentVehicleIndex];
        }
    }
}