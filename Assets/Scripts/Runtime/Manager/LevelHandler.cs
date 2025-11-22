using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BusUnjam
{
    [DisallowMultipleComponent]
    public class LevelHandler : MonoBehaviour
    {
        private const float PASSENGER_VELOCITY = 2f;
        
        [SerializeField] private Transform _cellContainer;
        
        private readonly List<Passenger> _waitingPassengers = new(); // who's waiting (null = not wait/not occupied)
        private readonly Queue<Passenger> _readyPassengers = new(); // who's ready to get on the vehicle :)))

        private int _rows;
        private int _columns;

        // Manual enable events in GameManager
        public void EnableSignal()
        {
            // Load
            GameManager.gridManager.OnAllLevelCellLoaded += AllLevelCellLoadHandle;
            GameManager.gridManager.OnAllLevelWaitingTileLoaded += AllLevelWaitingTileLoadHandle;
            GameManager.passengerManager.OnAllLevelPassengerLoaded += AllLevelPassengerLoadHandle;
            GameManager.vehicleManager.OnAllLevelVehicleLoaded += AllLevelVehicleLoadHandle;
        }
        
        // Auto disable events
        private void OnDestroy()
        {
            // Load
            GameManager.gridManager.OnAllLevelCellLoaded -= AllLevelCellLoadHandle;
            GameManager.gridManager.OnAllLevelWaitingTileLoaded -= AllLevelWaitingTileLoadHandle;
            GameManager.passengerManager.OnAllLevelPassengerLoaded -= AllLevelPassengerLoadHandle;
            GameManager.vehicleManager.OnAllLevelVehicleLoaded -= AllLevelVehicleLoadHandle;
        }

        private void Update()
        {
            HandleMouseClick();
        }
        
        public async UniTask LoadLevelData(LevelData levelData)
        {
            _rows = levelData.rows;
            _columns = levelData.columns;

            for (int i = 0; i < levelData.waitAreaSize; i++) _waitingPassengers.Add(null);

            UniTask[] tasks = {
                GameManager.gridManager.LoadCellFromLevelAsync(levelData),
                GameManager.gridManager.LoadWaitingTileAsync(levelData),
                GameManager.passengerManager.LoadPassengerFromLevelAsync(levelData),
                GameManager.vehicleManager.LoadVehicleFromLevelAsync(levelData),
            };
            await UniTask.WhenAll(tasks);
        }

        private void AllLevelCellLoadHandle()
        {
            Debug.Log("All level's cells loaded.");
        }
        
        private void AllLevelWaitingTileLoadHandle()
        {
            Debug.Log("All level's waiting tiles loaded.");
        }
        
        private void AllLevelPassengerLoadHandle()
        {
            Debug.Log("All level's passengers loaded.");
        }
        
        private void AllLevelVehicleLoadHandle()
        {
            Debug.Log("All level's vehicles loaded.");
        }

        private void HandleMouseClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                int checkLayers = LayerMask.GetMask("Passenger", "Cell");
                if (Physics.Raycast(ray, out hit, 100f, checkLayers))
                {
                    string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                    if (layerName == "Passenger")
                    {
                        Passenger p = hit.collider.GetComponent<Passenger>();
                        if (p != null)
                        {
                            var pos = GameManager.passengerManager.GetGridPositionOfPassenger(p);
                            if (pos == null) return;
                            GridClickHandleAsync(pos.Value.x, pos.Value.y).Forget();
                        }
                    }
                    else if (layerName == "Cell")
                    {
                        Cell c = hit.collider.GetComponent<Cell>();
                        if (c != null)
                        {
                            var pos = GameManager.gridManager.GetGridPositionOfCell(c);
                            if (pos == null) return;
                            GridClickHandleAsync(pos.Value.x, pos.Value.y).Forget();
                        }
                    }
                }
            }
        }

        private async UniTask GridClickHandleAsync(int r, int c)
        {
            if (GameManager.IsGameOver()) return;
            GridManager gridMgrRef = GameManager.gridManager;
            var path = gridMgrRef.FindPathToFirstRow(r, c);
            if (path == null) return;
            
            gridMgrRef.MarkCellEmpty(r, c);
            Passenger p = GameManager.passengerManager.GetPassengerAtGridPosition(r, c);
            
            // Already at exit row
            if (path.Count == 0) await HandlePassengerArrived(p, r);
            else
            {
                // Move the passenger along the path towards the exit
                foreach (Vector2Int step in path)
                {
                    Vector3 worldPosition = Utilities.GridToWorldXZNeg(_columns, step.x, step.y, Constants.CellDistance, _cellContainer.position);
                    float distance = Vector3.Distance(worldPosition, p.transform.position);
                    float duration = distance / PASSENGER_VELOCITY;
                    p.SetRunning(true);
                    await p.MoveTo(worldPosition, duration);
                }
                await HandlePassengerArrived(p, r);
            }
        }

        private async UniTask HandlePassengerArrived(Passenger p, int fromRow)
        {
            if (fromRow != 0) return; // Only process if at first row
            bool isMoving = GameManager.vehicleManager.IsVehicleMoving();
            if (isMoving)
            {
                // Vehicle is currently arriving –> divert passenger to waiting if possible
                if (HasFreeWaitingSlot()) await GoToWaitingArea(p);
                // No waiting slot free, hold in queue until vehicle arrives
                else
                {
                    _readyPassengers.Enqueue(p);
                    Debug.Log("add to ready and bus is moving");
                }
            }
            else
            {
                // Vehicle is waiting – handle immediately
                _readyPassengers.Enqueue(p);
                Debug.Log("add to ready and bus is not moving");
                await ProcessReadyPassengers();
            }
        }

        private async UniTask ProcessReadyPassengers()
        {
            VehicleManager vehMgrRef = GameManager.vehicleManager;
            bool isMoving = vehMgrRef.IsVehicleMoving();
            if (isMoving) return;

            while (HasReadyPassenger())
            {
                Passenger p = _readyPassengers.Peek();
                Vehicle v = vehMgrRef.GetCurrentVehicle();
                if (v == null) return;
                bool valid = vehMgrRef.HasBuses() && v.CanAddToVehicle(p);
                
                // If passenger cannot board and no waiting space, stop processing until a bus departs
                if (!valid && !HasFreeWaitingSlot()) break;
                
                // Dequeue now that we know it can be processed
                p = _readyPassengers.Dequeue();
                valid = vehMgrRef.HasBuses() && v.CanAddToVehicle(p);
                
                // TODO: Board the passenger onto the current bus (with tweens)
                if (valid)
                {
                    v.TryAddPassenger(p);
                    p.TriggerSitting();
                }
                // Send the passenger to the waiting area
                else await GoToWaitingArea(p);
            }
            
            Debug.Log("reached process ready");
        }

        private async UniTask GoToWaitingArea(Passenger p)
        {
            (int, Vector3) freeInfo = GetNearestFreeWaitingInfoForPassenger(p);
            int index = freeInfo.Item1;
            if (index == -1) 
            {
                Debug.LogError("No free waiting slot but function was called.");
                return;
            }
            Vector3 position = freeInfo.Item2;
            _waitingPassengers[index] = p;
            
            float distance = Vector3.Distance(position, p.transform.position);
            float duration = distance / PASSENGER_VELOCITY;
            
            p.SetRunning(true);
            await p.MoveTo(position, duration);
            p.SetRunning(false);
            
            // Check for level failed
            Debug.Log("reached goto waiting area / level fail check");
        }

        private bool HasReadyPassenger() => _readyPassengers.Count > 0;

        private void EmptyFreeWaitingIndex(int index) => _waitingPassengers[index] = null;
        
        private bool HasFreeWaitingSlot()
        {
            foreach (Passenger p in _waitingPassengers)
            {
                if (p == null) return true;
            }
            return false;
        }

        private List<int> GetFreeWaitingIndexes()
        {
            if (!HasFreeWaitingSlot()) return null;
            List<int> slots = new();
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                if (_waitingPassengers[i] == null)
                    slots.Add(i);
            }
            return slots;
        }

        private (int, Vector3) GetNearestFreeWaitingInfoForPassenger(Passenger p)
        {
            List<int> freeSlots = GetFreeWaitingIndexes();
            if (freeSlots == null || freeSlots.Count == 0) return (-1, Vector3.zero);
            
            int freeSlotCount = freeSlots.Count;
            float smallestDistance = float.MaxValue;
            int nearestIndex = -1;
            Vector3 nearestPosition = Vector3.zero;
            
            for (int i = 0; i < freeSlotCount; i++)
            {
                int slot = freeSlots[i];
                Vector3? tilePos = GameManager.gridManager.GetPositionOfWaitingTileIndex(slot);
                if (tilePos == null) continue;

                float dist = Vector3.Distance(tilePos.Value, p.transform.position);
                if (dist < smallestDistance)
                {
                    smallestDistance = dist;
                    nearestIndex = slot;
                    nearestPosition = tilePos.Value;
                }
            }
            return (nearestIndex, nearestPosition);
        }
    }
}