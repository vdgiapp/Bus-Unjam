using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class LevelHandler : MonoBehaviour
    {
        public static LevelHandler instance { get; private set; }
        
        [SerializeField] private Transform _cellContainer;

        [SerializeField] private TMP_Text _tempTmpText;
        [SerializeField] private TMP_Text _fpsTmpText;
        
        [Header("Managers")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private PassengerManager passengerManager;
        [SerializeField] private VehicleManager vehicleManager;
        
        private readonly List<Passenger> _waitingPassengers = new();
        
        private int _passengersOnVehicle = 0;
        
        private int _rows;
        private int _columns;

        private bool _isLevelInit = false;
        private bool _isLevelEnded = false;
        
        private int _frameCount = 0;
        private float _fpsTimer = 0f;

        private void Awake()
        {
            instance = this;

            vehicleManager.OnVehicleArrived += VehicleArrivedHandle;
            vehicleManager.OnAllVehicleDone += LevelCompleteHandle;
        }
        
        private void Update() 
        {
            PlayerUpdate();;
            FPSUpdate();
        }

        private void PlayerUpdate()
        {
            if (!_isLevelInit || _isLevelEnded) return;
            bool clicked = false;
            if (Input.GetMouseButtonDown(0)) clicked = true; // PC or Editor
            if (Input.touchCount > 0) // Mobile
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Ended) clicked = true;
            }
            if (!clicked) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int checkLayers = LayerMask.GetMask(Constants.LAYER_NAME_PASSENGER);
            if (Physics.Raycast(ray, out RaycastHit hit, Constants.MAX_RAYCAST_DISTANCE, checkLayers))
            {
                Passenger p = hit.collider.GetComponent<Passenger>();
                if (p != null)
                {
                    if (IsPassengerTagMoving(p) || IsPassengerTagWaiting(p) || IsPassengerTagSitting(p)) return;
                    Vector2Int? pos = passengerManager.GetGridPositionOfPassenger(p);
                    if (pos == null)
                    {
                        Debug.LogWarning($"Can't find grid position of Passenger {p}");
                        return;
                    }
                    _ = GridSelectedHandleAsync(pos.Value.x, pos.Value.y);
                }
            }
        }

        private void OnDestroy()
        {
            vehicleManager.OnVehicleArrived -= VehicleArrivedHandle;
            vehicleManager.OnAllVehicleDone -= LevelCompleteHandle;
        }

        public async UniTask InitLevel(LevelData levelData)
        {
            _rows = levelData.rows;
            _columns = levelData.columns;

            for (int i = 0; i < levelData.waitAreaSize; i++)
            {
                _waitingPassengers.Add(null);
            }
            await UniTask.WhenAll(
                gridManager.LoadCellFromLevelAsync(levelData), 
                gridManager.LoadWaitingTileAsync(levelData), 
                passengerManager.LoadPassengerFromLevelAsync(levelData), 
                vehicleManager.LoadVehicleFromLevelAsync(levelData)
            );
            _isLevelInit = true;
        }
        
        
        private void FPSUpdate()
        {
            _frameCount++;
            _fpsTimer += Time.deltaTime;

            if (_fpsTimer >= 1f)
            {
                int fps = Mathf.RoundToInt(_frameCount / _fpsTimer);

                _fpsTmpText.text = $"FPS: {fps}";

                _frameCount = 0;
                _fpsTimer = 0f;
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        private async UniTask GridSelectedHandleAsync(int r, int c)
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
            
            // check trung mau vehicle, neu trung thi di chuyen len vehicle luon
            
            
            // khong thi di chuyen qua hang doi
            Vector3 endFirstRowPosition = (pathToFirstRow.Count == 0)
                ? Utilities.GridToWorldXZNeg(_columns, 0, c, Constants.CELL_DISTANCE, _cellContainer.position) 
                : Utilities.GridToWorldXZNeg(_columns, pathToFirstRow[^1].x, pathToFirstRow[^1].y, Constants.CELL_DISTANCE, _cellContainer.position);

            (int, Vector3) freeInfo = GetNearestEmptyWaiting(endFirstRowPosition);
            int nearestWaitingIndex = freeInfo.Item1;
            Vector3 nearestWaitingPosition = freeInfo.Item2;

            if (nearestWaitingIndex == -1)
            {
                Debug.LogWarning("Can't find nearest waiting tile index");
                return;
            }
            await MovePassengerAlongPathAsync(p, pathToFirstRow, r, c, nearestWaitingIndex);
            await MovePassengerToWaitingAreaAsync(p, nearestWaitingPosition);
            
            Vehicle v = vehicleManager.GetCurrentVehicle();
            if (v == null) return;
            if (vehicleManager.IsVehiclesMoving()) return;
            
            int seatIndex = GetEmptySeatIndex(p, v);
            if (seatIndex == -1)
            {
                await CheckLevelFailedFullWaiting();
            }
            else
            {
                if (!IsPassengerTagWaiting(p)) return;
                await MovePassengerToVehicleAsync(p, v, nearestWaitingIndex, seatIndex);
                await NextVehicleCheckAsync(v);
            }  
        }
        
        private async UniTask MovePassengerAlongPathAsync(Passenger p, IReadOnlyList<Vector2Int> path, int r, int c, int nearestIndex)
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

        private async UniTask MovePassengerToWaitingAreaAsync(Passenger p, Vector3 waitingPosition)
        {
            p.SetRunningAnimation(true);
            await p.MoveTo(waitingPosition, GetMoveDuration(p.transform.position, waitingPosition, Constants.PASSENGER_MOVE_SPEED));
            p.SetRunningAnimation(false);
            SetPassengerTagWaiting(p);
        }

        private async UniTask MovePassengerToVehicleAsync(Passenger p, Vehicle v, int nearestIndex, int seatIndex)
        {
            _waitingPassengers[nearestIndex] = null;
            v.data.occupied[seatIndex] = true;
            p.SetRunningAnimation(true);
            Vector3 doorPosition = v.GetDoorTransform().position;
            Vector3 destination = new(doorPosition.x, p.transform.position.y, doorPosition.z);
            await p.MoveTo(destination, GetMoveDuration(p.transform.position, destination, Constants.PASSENGER_MOVE_SPEED));
            _passengersOnVehicle++;
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
            _passengersOnVehicle = 0;
            await CheckLevelFailedFullWaiting();
            
            List<UniTask> tasks = new();
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                Passenger p = _waitingPassengers[i];
                if (p == null) continue;
                int seatIndex = GetEmptySeatIndex(p, v);
                if (seatIndex == -1) continue;
                if (!IsPassengerTagWaiting(p)) continue;
                tasks.Add(MovePassengerToVehicleAsync(p, v, i, seatIndex));
            }
            await UniTask.WhenAll(tasks);
            await NextVehicleCheckAsync(v);
        }

        private async UniTask NextVehicleCheckAsync(Vehicle v)
        {
            if (IsVehicleFull(v) && _passengersOnVehicle >= Constants.VEHICLE_SEAT_SLOTS)
            {
                await vehicleManager.NextVehicleAsync();
            }
        }
        
        private async UniTask CheckLevelFailedFullWaiting()
        {
            Vehicle v = vehicleManager.GetCurrentVehicle();
            if (v == null) return;
            if (!IsWaitingFull() || vehicleManager.IsVehiclesMoving()) return;
            await UniTask.WaitForSeconds(Constants.FAILED_TIME_CHECK);
            if (!IsWaitingFull() || vehicleManager.IsVehiclesMoving()) return;
            if (!HasValidPassengerForVehicle(v))
            {
                LevelFailedHandle();
            }
        }
        
        // Event
        private void LevelCompleteHandle()
        {
            if (_isLevelEnded) return;
            _isLevelEnded = true;
            Debug.Log("Level completed");
            _tempTmpText.text = "Level Completed";
        }
        
        private void LevelFailedHandle()
        {
            if (_isLevelEnded) return;
            _isLevelEnded = true;
            Debug.Log("Level failed");
            _tempTmpText.text = "Level Failed";
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
        
        private bool HasValidPassengerForVehicle(Vehicle v)
        {
            foreach (var p in _waitingPassengers)
            {
                if (p == null) continue;
                if (GetEmptySeatIndex(p, v) != -1) return true;
            }
            return false;
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