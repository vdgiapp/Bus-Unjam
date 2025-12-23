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
        private readonly List<Vehicle> _activeVehicles = new();
        private readonly List<VehicleData> _pendingVehicleData = new();
        
        private bool _isVehiclesMoving;
        
        private Sequence _vehicleMoveSequence;
        
        public async UniTask LoadVehicleFromLevelAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            
            // Initialize vehicle pool
            GameObject prefab = GameManager.GetCurrentTheme()?.vehiclePrefab();
            GameObject[] loaded = await InstantiateAsync(prefab, Constants.VEHICLE_POOL_SIZE, _vehicleContainer).ToUniTask();
            
            // Add vehicles to pool
            foreach (GameObject go in loaded)
            {
                Vehicle vehicle = go.GetComponent<Vehicle>();
                vehicle.gameObject.SetActive(false);
                _vehiclePool.Add(vehicle);
            }
            
            // Spawn initial vehicles
            foreach (VehicleData data in levelData.vehicles)
            {
                if (!TrySpawnVehicle(data))
                {
                    _pendingVehicleData.Add(data);
                }
            }
        }
        
        public Sequence NextVehicle()
        {
            if (!CanMoveToNextVehicle()) return null;
            
            // Set flag
            _isVehiclesMoving = true;
            
            // Vehicle movement sequence
            _vehicleMoveSequence?.Kill();
            _vehicleMoveSequence = DOTween.Sequence();
            _vehicleMoveSequence.SetAutoKill();
            
            for (int i = 0; i < _activeVehicles.Count; i++)
            {
                Vector3 targetPosition = Vector3.zero;
                Vehicle vehicle = _activeVehicles[i];
                if (i == 0)
                {
                    // Position off-screen to the right
                    targetPosition.x = 2.5f * Constants.VEHICLE_DISTANCE;
                    
                    // Move first vehicle out of screen
                    _vehicleMoveSequence.Join(vehicle.MoveLocalTo(targetPosition, Constants.VEHICLE_MOVE_DURATION));
                }
                else
                {
                    // Position in queue
                    targetPosition.x = -1f * (i - 1) * Constants.VEHICLE_DISTANCE;
                    
                    // Move other vehicles forward
                    _vehicleMoveSequence.Join(vehicle.MoveLocalTo(targetPosition, Constants.VEHICLE_MOVE_DURATION, Ease.OutQuad));
                }
            }
            
            _vehicleMoveSequence.onComplete += OnVehicleMoveSequenceComplete;
            return _vehicleMoveSequence;
        }
        
        private void OnVehicleMoveSequenceComplete()
        {
            // Remove first vehicle
            Vehicle firstVehicle = _activeVehicles[0];
                
            // Return vehicle to pool
            firstVehicle.transform.DOKill();
            firstVehicle.gameObject.SetActive(false);
                
            // Remove from active list
            _activeVehicles.RemoveAt(0);
                
            // Reset flag
            _isVehiclesMoving = false;
                
            // Notify vehicle arrival
            Vehicle currentVehicle = GetCurrentVehicle();
            if (currentVehicle != null) OnVehicleArrived?.Invoke(currentVehicle);
                
            // Try to spawn new vehicle if there are any pending
            bool spawned = TrySpawnNextVehicle();
            bool hasMoreVehicles = HasVehiclesRemaining();
            
            // Notify level complete if no more vehicles to spawn
            if (!spawned && !hasMoreVehicles) OnAllVehicleDone?.Invoke();
        }

        private bool TrySpawnNextVehicle()
        {
            if (!HasPendingVehicles()) return false;
            
            for (int i = 0; i < _pendingVehicleData.Count; i++)
            {
                VehicleData data = _pendingVehicleData[i];
                if (TrySpawnVehicle(data))
                {
                    _pendingVehicleData.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        private bool TrySpawnVehicle(VehicleData data)
        {
            Vehicle vehicle = GetAvailableVehicleFromPool();
            if (vehicle == null) return false;

            // Clear vehicle seats
            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                Transform seat = vehicle.GetSeatTransformAtIndex(i);
                foreach (Transform child in seat)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    //Destroy(child.gameObject);
                }
            }
            
            // Configure vehicle
            vehicle.data = data;
            vehicle.SetColor(GameManager.GetColorByType(data.colorType));
            
            // Position vehicle
            float xPosition = -1f * _activeVehicles.Count * Constants.VEHICLE_DISTANCE;
            vehicle.transform.localPosition = new Vector3(xPosition, 0f, 0f);
            
            // Activate vehicle
            vehicle.gameObject.SetActive(true);
            _activeVehicles.Add(vehicle);
            
            return true;
        }

        private Vehicle GetAvailableVehicleFromPool()
        {
            foreach (Vehicle vehicle in _vehiclePool)
            {
                if (!vehicle.gameObject.activeSelf)
                {
                    return vehicle;
                }
            }
            return null;
        }
        
        private bool IsValidLevelData(LevelData levelData)
        {
            return levelData.vehicles is { Count: > 0 };
        }

        private bool CanMoveToNextVehicle()
        {
            return HasActiveVehicles() && !IsVehiclesMoving() && GetCurrentVehicle() != null;
        }

        private bool HasActiveVehicles()
        {
            return _activeVehicles.Count > 0;
        }
        
        private bool HasPendingVehicles()
        {
            return _pendingVehicleData.Count > 0;
        }
        
        private bool HasVehiclesRemaining()
        {
            return HasActiveVehicles() || HasPendingVehicles();
        }

        public Vehicle GetCurrentVehicle()
        {
            return HasActiveVehicles() ? _activeVehicles[0] : null;
        }

        public Vehicle GetNextVehicle()
        {
            return _activeVehicles.Count > 1 ? _activeVehicles[1] : null;
        }
                
        public bool IsVehiclesMoving()
        {
            return _isVehiclesMoving;
        }
    }
}