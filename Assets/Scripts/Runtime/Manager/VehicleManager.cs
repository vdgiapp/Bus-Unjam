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
        public event Action<Vehicle> OnVehicleArrived;
        public event Action OnAllVehicleDone; // level complete

        [SerializeField] private Transform _vehicleContainer;
        
        private readonly List<Vehicle> _vehiclePool = new();
        private readonly List<Vehicle> _vehicleActive = new();
        private readonly List<VehicleData> _remainVehicleData = new();
        private Vehicle _currentVehicle = null;
        private bool _isVehiclesMoving = false;

        public async UniTask LoadVehicleFromLevelAsync(LevelData levelData)
        {
            if (levelData == null || levelData.vehicles == null || levelData.vehicles.Length == 0) return;
            
            GameObject prefab = GameManager.GetCurrentTheme().GetVehiclePrefab();
            GameObject[] loaded = 
                await InstantiateAsync(prefab, Constants.VEHICLE_POOL_SIZE, _vehicleContainer)
                    .ToUniTask();
            
            // Add to vehicle pool and hide it
            foreach (GameObject go in loaded)
            {
                Vehicle v = go.GetComponent<Vehicle>();
                v.gameObject.SetActive(false);
                _vehiclePool.Add(v);
            }

            // Spawn vehicle from level data
            foreach (VehicleData data in levelData.vehicles)
            {
                if (!TrySpawnNewVehicleFromPool(data))
                {
                    _remainVehicleData.Add(data);
                }
            }

            if (GetActiveVehicles() > 0) _currentVehicle = _vehicleActive[0];
            else _currentVehicle = null;
        }
        
        public async UniTask NextVehicleAsync()
        {
            if (GetActiveVehicles() == 0 || IsVehiclesMoving()) return;
            if (_currentVehicle != null)
            {
                await MoveToNextDestinationAsync();
                if (!TrySpawnNewVehicle() && !HasBuses())
                {
                    OnAllVehicleDone?.Invoke();
                }
            }
        }

        private bool TrySpawnNewVehicle()
        {
            if (GetRemainLevelVehicles() == 0) return false;
            for (int i = 0; i < GetRemainLevelVehicles(); i++)
            {
                VehicleData data = _remainVehicleData[i];
                if (TrySpawnNewVehicleFromPool(data))
                {
                    _remainVehicleData.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        private bool TrySpawnNewVehicleFromPool(VehicleData data)
        {
            Vehicle veh = GetVehicleFromPool();
            if (veh == null) return false;
            
            // destroy passenger (or anything else) in vehicle's seats
            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                Transform seat = veh.GetSeatTransformAtIndex(i);
                foreach (Transform child in seat)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
            
            veh.data = data;
            veh.SetColor(GameManager.GetColorByType(data.colorType));
            veh.transform.localPosition = new Vector3(-1f * GetActiveVehicles() * Constants.VEHICLE_DISTANCE, 0.0f, 0.0f);
            veh.gameObject.SetActive(true);
            _vehicleActive.Add(veh);
            return true;
        }
        
        private Vehicle GetVehicleFromPool()
        {
            Vehicle ret = null;
            foreach (Vehicle veh in _vehiclePool)
            {
                if (veh.gameObject.activeSelf) continue;
                ret = veh;
                break;
            }
            return ret;
        }
        
        private void ReturnVehicleToPool(Vehicle veh)
        {
            veh.transform.DOKill();
            veh.gameObject.SetActive(false);
        }
        
        private async UniTask MoveToNextDestinationAsync()
        {
            if (GetActiveVehicles() == 0 || IsVehiclesMoving()) return;
            _isVehiclesMoving = true;
            
            int index = 0;
            UniTask[] tasks = new UniTask[GetActiveVehicles()];
            foreach (Vehicle veh in _vehicleActive)
            {
                tasks[index] = MoveVehicleIndexAsync(veh, index);
                index++;
            }
            await UniTask.WhenAll(tasks);
            
            ReturnVehicleToPool(_vehicleActive[0]);
            _vehicleActive.RemoveAt(0);
            
            if (GetActiveVehicles() > 0) _currentVehicle = _vehicleActive[0];
            else _currentVehicle = null;
            
            _isVehiclesMoving = false;
            
            if (_currentVehicle != null)
            {
                OnVehicleArrived?.Invoke(_currentVehicle);
            }
        }

        private async UniTask MoveVehicleIndexAsync(Vehicle veh, int index)
        {
            // Move first vehicle out of screen
            Vector3 pos = veh.transform.localPosition;
            if (index == 0)
            {
                pos.x = 2.5f * Constants.VEHICLE_DISTANCE;
                await veh.MoveLocalTo(pos, Constants.VEHICLE_MOVE_DURATION);
            }
            else
            {
                pos.x = -1.0f * (index - 1) * Constants.VEHICLE_DISTANCE;
                await veh.MoveLocalTo(pos, Constants.VEHICLE_MOVE_DURATION, Ease.OutQuad);
            }
        }
        
        private bool HasBuses()
        {
            return GetActiveVehicles() > 0 || GetRemainLevelVehicles() > 0;
        }
        
        private int GetRemainLevelVehicles()
        {
            return _remainVehicleData.Count;
        }
        
        private int GetActiveVehicles()
        {
            return _vehicleActive.Count;
        }
        
        public Vehicle GetCurrentVehicle()
        {
            return _currentVehicle;
        }
        
        public bool IsVehiclesMoving()
        {
            return _isVehiclesMoving;
        }

    }
}