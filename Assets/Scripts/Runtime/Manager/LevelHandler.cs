using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class LevelHandler : MonoBehaviour
    {
        public static LevelHandler instance { get; private set; }
        
        [SerializeField] private Transform _cellContainer;
        
        [Header("Managers")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private PassengerManager passengerManager;
        [SerializeField] private VehicleManager vehicleManager;
        
        private readonly List<Passenger> _waitingPassengers = new();

        private int _currentCount = 0;
        
        private int _rows;
        private int _columns;

        private bool _isLevelInit = false;
        private bool _isLevelEnded = false;

        private void Awake()
        {
            instance = this;

            vehicleManager.OnVehicleArrived += VehicleArrivedHandle;
            vehicleManager.OnAllVehicleDone += AllVehicleDoneHandle;
        }

        private void Update()
        {
            if (_isLevelInit && !_isLevelEnded && Input.GetMouseButtonDown(0))
            {
                PlayerClickedHandle();
            }
        }

        private void OnDestroy()
        {
            vehicleManager.OnVehicleArrived -= VehicleArrivedHandle;
            vehicleManager.OnAllVehicleDone -= AllVehicleDoneHandle;
        }

        public async UniTask InitLevel(LevelData levelData)
        {
            _rows = levelData.rows;
            _columns = levelData.columns;

            for (int i = 0; i < levelData.waitAreaSize; i++)
            {
                _waitingPassengers.Add(null);
            }

            UniTask[] tasks = {
                gridManager.LoadCellFromLevelAsync(levelData),
                gridManager.LoadWaitingTileAsync(levelData),
                passengerManager.LoadPassengerFromLevelAsync(levelData),
                vehicleManager.LoadVehicleFromLevelAsync(levelData),
            };
            await UniTask.WhenAll(tasks);
            _isLevelInit = true;
        }
        
        // Event
        // ReSharper disable Unity.PerformanceAnalysis
        private void PlayerClickedHandle()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int checkLayers = LayerMask.GetMask(Constants.LAYER_NAME_PASSENGER);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Constants.MAX_RAYCAST_DISTANCE, checkLayers))
            {
                Passenger p = hit.collider.GetComponent<Passenger>();
                if (p != null)
                {
                    if (IsPassengerTagMoving(p))
                    {
                        Debug.Log("That passenger is moving");
                        return;
                    }
                    if (IsPassengerTagWaiting(p))
                    {
                        Debug.Log("That passenger is waiting for vehicle");
                        return;
                    }
                    if (IsPassengerTagSitting(p))
                    {
                        Debug.Log("That passenger is sitting on vehicle");
                        return;
                    }
                    Vector2Int? pos = passengerManager.GetGridPositionOfPassenger(p);
                    if (pos == null)
                    {
                        Debug.LogWarning($"Can't find grid position of Passenger {p}");
                        return;
                    }
                    _ = GridSelectedHandle(pos.Value.x, pos.Value.y);
                }
            }
        }

        private async UniTask GridSelectedHandle(int r, int c)
        {
            IReadOnlyList<Vector2Int> pathToFirstRow = gridManager.GetPathToFirstRow(r, c);
            Passenger p = passengerManager.GetPassengerAtGridPosition(r, c);

            if (pathToFirstRow == null)
            {
                Debug.Log("There's no way out for that passenger :)");
                _ = p.Shake();
                return;
            }
            if (IsWaitingFull())
            {
                Debug.Log("Waiting queue is full of passengers, please wait :(");
                _ = p.Shake();
                return;
            }
            
            Vector3 endPosition = (pathToFirstRow.Count == 0)
                ? Utilities.GridToWorldXZNeg(_columns, 0, c, Constants.CELL_DISTANCE, _cellContainer.position) 
                : Utilities.GridToWorldXZNeg(_columns, pathToFirstRow[^1].x, pathToFirstRow[^1].y, Constants.CELL_DISTANCE, _cellContainer.position);

            (int, Vector3) freeInfo = GetNearestEmptyWaiting(endPosition);
            int nearestIndex = freeInfo.Item1;
            Vector3 nearestPosition = freeInfo.Item2;

            if (nearestIndex == -1)
            {
                Debug.LogWarning("Can't find nearest waiting tile index");
                return;
            }
            await MovePassengerAlongPath(p, pathToFirstRow, r, c, nearestIndex);
            await MovePassengerToWaitingArea(p, nearestPosition);
            
            // Check for game over here
            
            //
            
            Vehicle v = vehicleManager.GetCurrentVehicle();
            if (v == null) return;
            if (vehicleManager.IsVehiclesMoving()) return;
            
            int seatIndex = CanAddToVehicle(p, v);
            if (seatIndex == -1) return;
            await MovePassengerToVehicle(p, v, nearestIndex, seatIndex);
            await PerformNextVehicleCondition(v);
        }
        
        private async UniTask MovePassengerAlongPath(Passenger p, IReadOnlyList<Vector2Int> path, int r, int c, int nearestIndex)
        {
            _waitingPassengers[nearestIndex] = p;
            gridManager.MarkCellEmpty(r, c);
            SetPassengerTagMoving(p);
            foreach (Vector2Int step in path)
            {
                Vector3 worldPosition = Utilities.GridToWorldXZNeg(_columns, step.x, step.y, Constants.CELL_DISTANCE, _cellContainer.position);
                p.SetRunningAnimation(true);
                await p.MoveTo(worldPosition, GetMoveDuration(p.transform.position, worldPosition, Constants.PASSENGER_MOVE_SPEED));
            }
        }

        private async UniTask MovePassengerToWaitingArea(Passenger p, Vector3 waitingPosition)
        {
            p.SetRunningAnimation(true);
            await p.MoveTo(waitingPosition, GetMoveDuration(p.transform.position, waitingPosition, Constants.PASSENGER_MOVE_SPEED));
            p.SetRunningAnimation(false);
            SetPassengerTagWaiting(p);
        }

        private async UniTask MovePassengerToVehicle(Passenger p, Vehicle v, int nearestIndex, int seatIndex)
        {
            _waitingPassengers[nearestIndex] = null;
            v.data.occupied[seatIndex] = true;
            p.SetRunningAnimation(true);
            Vector3 doorPosition = v.GetDoorTransform().position;
            Vector3 destination = new(doorPosition.x, p.transform.position.y, doorPosition.z);
            await p.MoveTo(destination, GetMoveDuration(p.transform.position, destination, Constants.PASSENGER_MOVE_SPEED));
            _currentCount++;
            p.transform.SetParent(v.GetSeatTransformAtIndex(seatIndex));
            p.transform.localPosition = Vector3.zero;
            p.TriggerSittingAnimation();
            SetPassengerTagSitting(p);
        }

        // Event
        private void VehicleArrivedHandle(Vehicle v)
        {
            _ = VehicleArrivedHandleAsync(v);
        }

        private async UniTask VehicleArrivedHandleAsync(Vehicle v)
        {
            List<UniTask> tasks = new();
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                Passenger p = _waitingPassengers[i];
                if (p == null) continue;
                
                int seatIndex = CanAddToVehicle(p, v);
                if (seatIndex == -1) continue;
                
                tasks.Add(MovePassengerToVehicle(p, v, i, seatIndex));
            }
            await UniTask.WhenAll(tasks);
            await PerformNextVehicleCondition(v);
        }

        private async UniTask PerformNextVehicleCondition(Vehicle v)
        {
            if (IsVehicleFull(v) && _currentCount >= Constants.VEHICLE_SEAT_SLOTS)
            {
                await vehicleManager.NextVehicleAsync();
                _currentCount = 0;
            }
        }
        
        // Event
        private void AllVehicleDoneHandle()
        {
            // Check for game win here
            
            //
        }

        private int CanAddToVehicle(Passenger p, Vehicle v)
        {
            if (!IsPassengerTagWaiting(p)) return -1;
            int index = GetEmptySeatIndex(p, v);
            return index;
        }

        // Vehicle helper functions
        private bool IsVehicleFull(Vehicle v)
        {
            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++) if (!v.data.occupied[i]) return false;
            return true;
        }
        
        private int GetEmptySeatIndex(Passenger p, Vehicle v)
        {
            if (IsVehicleFull(v) || p == null || v == null) return -1;
            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                if (v.data.occupied[i]) continue;
                if (v.data.colorType == p.data.colorType) return i;
            }
            return -1;
        }
        
        // Passenger helper functions
        private void SetPassengerTagWaiting(Passenger p)
        {
            p.tag = Constants.TAG_NAME_WAITING;
        }
        
        private void SetPassengerTagMoving(Passenger p)
        {
            p.tag = Constants.TAG_NAME_MOVING;
        }
        
        private void SetPassengerTagSitting(Passenger p)
        {
            p.tag = Constants.TAG_NAME_SITTING;
        }
        
        private bool IsPassengerTagWaiting(Passenger p)
        {
            return p.gameObject.CompareTag(Constants.TAG_NAME_WAITING);
        }

        private bool IsPassengerTagMoving(Passenger p)
        {
            return p.gameObject.CompareTag(Constants.TAG_NAME_MOVING);
        }
        
        private bool IsPassengerTagSitting(Passenger p)
        {
            return p.gameObject.CompareTag(Constants.TAG_NAME_SITTING);
        }
        
        // Waiting queue helper functions
        private bool IsWaitingFull()
        {
            foreach (Passenger p in _waitingPassengers) if (p == null) return false;
            return true;
        }

        private (int, Vector3) GetNearestEmptyWaiting(Vector3 fromPosition)
        {
            float smallestDistance = float.MaxValue;
            int nearestIndex = -1;
            Vector3 nearestPosition = Vector3.zero;
            
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                if (_waitingPassengers[i] != null) continue;
                Vector3? tilePos = gridManager.GetPositionOfWaitingTileIndex(i);
                if (tilePos == null) continue;

                float dist = Vector3.Distance(tilePos.Value, fromPosition);
                if (dist < smallestDistance)
                {
                    smallestDistance = dist;
                    nearestIndex = i;
                    nearestPosition = tilePos.Value;
                }
            }
            
            if (nearestIndex == -1) return (-1, Vector3.zero);
            return (nearestIndex, nearestPosition);
        }
        
        // Calculate helper functions
        private float GetMoveDuration(Vector3 from, Vector3 to, float speed)
        {
            return Vector3.Distance(from, to) / speed;
        }
    }
}