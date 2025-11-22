using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace BusUnjam
{
    [DisallowMultipleComponent]
    public class VehicleManager : MonoBehaviour
    {
        public event Action OnAllLevelVehicleLoaded;
        public event Action<Vehicle> OnVehicleArrived;
        public event Action OnAllVehicleDone; // level complete

        [SerializeField] private float _moveDuration = 2.0f;
        [SerializeField] private Transform _vehicleContainer;
        
        private readonly List<Vehicle> _vehiclePool = new();
        private readonly List<Vehicle> _vehicleActive = new();
        private readonly List<VehicleData> _remainVehicleData = new();
        private Vehicle _currentVehicle = null;
        private bool _isVehicleMoving = false;

        public async UniTask LoadVehicleFromLevelAsync(LevelData levelData)
        {
            if (levelData == null || levelData.vehicles == null || levelData.vehicles.Length == 0) return;
            
            GameObject prefab = GameManager.GetCurrentTheme().GetVehiclePrefab();
            GameObject[] loaded = 
                await InstantiateAsync(prefab, Constants.VehiclePoolSize, _vehicleContainer)
                    .ToUniTask();
            
            // Add to vehicle pool and hide it
            foreach (GameObject go in loaded)
            {
                Vehicle v = go.GetComponent<Vehicle>();
                v.gameObject.SetActive(false);
                _vehiclePool.Add(v);
            }
            Debug.Log($"Initialized vehicle pool with {Constants.VehiclePoolSize} vehicles.");

            // Spawn vehicle from level data
            foreach (VehicleData data in levelData.vehicles)
            {
                if (!TrySpawnNewVehicleFromPool(data))
                {
                    _remainVehicleData.Add(data);
                }
            }
            _currentVehicle = (GetActiveVehicles() > 0) ? _vehicleActive[0] : null;
            
            OnAllLevelVehicleLoaded?.Invoke();
        }

        public bool TrySpawnNewVehicle()
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
        
        public async UniTask CheckCurrentVehicleAsync()
        {
            if (GetActiveVehicles() == 0 || IsVehicleMoving()) return;
            if (_currentVehicle != null && _currentVehicle.data != null && _currentVehicle.IsFull())
            {
                await MoveToNextDestinationAsync();
                bool spawned = TrySpawnNewVehicle();
                if (!spawned && !HasBuses())
                {
                    // Level complete
                    OnAllVehicleDone?.Invoke();
                }
            }
        }

        public bool HasBuses() => GetActiveVehicles() > 0 && GetRemainLevelVehicles() > 0;
        public int GetRemainLevelVehicles() => _remainVehicleData.Count;
        public int GetActiveVehicles() => _vehicleActive.Count;
        public Vehicle GetCurrentVehicle() => _currentVehicle;
        public bool IsVehicleMoving() => _isVehicleMoving;
        
        private bool TrySpawnNewVehicleFromPool(VehicleData data)
        {
            Vehicle veh = GetVehicleFromPool();
            if (veh == null) return false; // Active queue is full!
            
            veh.Reset();
            veh.data = data;
            
            veh.transform.localPosition = new Vector3(-1f * GetActiveVehicles() * Constants.VehicleDistance, 0.0f, 0.0f);
            veh.SetColor(GameManager.GetColorByType(data.colorType));
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
            if (GetActiveVehicles() == 0 || IsVehicleMoving()) return;
            _isVehicleMoving = true;
            
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
            
            _currentVehicle = (GetActiveVehicles() > 0) ? _vehicleActive[0] : null;
            _isVehicleMoving = false;
            
            OnVehicleArrived?.Invoke(_currentVehicle);
        }

        private async UniTask MoveVehicleIndexAsync(Vehicle veh, int index)
        {
            // Move first vehicle out of screen
            Vector3 pos = veh.transform.position;
            if (index == 0)
            {
                pos.x = 2.5f * Constants.VehicleDistance;
                await veh.MoveLocalTo(pos, _moveDuration);
            }
            else
            {
                pos.x = -1.0f * (index - 1) * Constants.VehicleDistance;
                await veh.MoveLocalTo(pos, _moveDuration, Ease.OutQuad);
            }
        }
    }
}