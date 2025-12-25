/*
 #region Passenger Selection Logic
        private async UniTask HandlePassengerSelectionAsync(int row, int col)
        {
            // TODO: Kiểm tra/xử lý các type khác nhau của Passenger

            // Kiểm tra đường đi và lấy thông tin path và passenger
            if (!TryGetPathAndPassenger(row, col, out var path, out var passenger))
            {
                passenger.Shake();
                return;
            }

            // Tìm vị trí hàng chờ trống gần nhất
            Vector3 endFirstRowPosition = CalculateEndPosition(path);
            (int waitingIndex, Vector3 waitingPosition) = GetNearestEmptyWaiting(endFirstRowPosition);

            if (waitingIndex == -1)
            {
                passenger.Shake();
                return;
            }

            // Xử lý logic chính dựa trên trạng thái xe
            if (vehicleManager.GetCurrentVehicle() == null) return;

            if (vehicleManager.IsVehiclesMoving())
            {
                await HandlePassengerWithMovingVehicleAsync(passenger, row, col, path, waitingIndex, waitingPosition);
            }
            else
            {
                await HandlePassengerWithStoppedVehicleAsync(passenger, row, col, path, waitingIndex, waitingPosition);
            }
        }

        private async UniTask HandlePassengerWithMovingVehicleAsync(Passenger passenger,
            int row, int col, IReadOnlyList<Vector2Int> path,
            int waitingIndex, Vector3 waitingPosition)
        {
            if (!await TryMovePassengerToWaitingAsync(passenger, row, col, path, waitingIndex, waitingPosition)) return;

            await UniTask.WaitUntil(() => !vehicleManager.IsVehiclesMoving());

            // Kiểm tra lại passenger vẫn còn đang chờ (chưa bị xử lý bởi OnVehicleArrived)
            if (_waitingPassengers[waitingIndex] != passenger) return;

            // Try to board
            await TryBoardVehicleFromWaitingAsync(passenger, waitingIndex);
        }

        private async UniTask HandlePassengerWithStoppedVehicleAsync(Passenger passenger,
            int row, int col, IReadOnlyList<Vector2Int> path,
            int waitingIndex, Vector3 waitingPosition)
        {
            int seatIndex = GetEmptySeatIndex(passenger, vehicleManager.GetCurrentVehicle());

            if (seatIndex == -1)
            {
                // No seat available, move to waiting area
                await MoveToWaitingAndCheckFailAsync(passenger, row, col, path, waitingIndex, waitingPosition);
            }
            else
            {
                // Seat available, board directly
                await BoardVehicleDirectlyAsync(passenger, row, col, path, seatIndex);
            }
        }
#endregion

#region Passenger Movement Operations
        private async UniTask<bool> TryMovePassengerToWaitingAsync(Passenger passenger,
            int row, int col, IReadOnlyList<Vector2Int> path,
            int waitingIndex, Vector3 waitingPosition)
        {
            if (IsWaitingFull())
            {
                passenger.Shake();
                return false;
            }

            _waitingPassengers[waitingIndex] = passenger;
            gridManager.MarkCellEmpty(row, col);
            SetPassengerTagMoving(passenger);

            await MovePassengerAlongPathAsync(passenger, path);
            await MovePassengerToWaitingAreaAsync(passenger, waitingPosition);

            SetPassengerTagWaiting(passenger);
            return true;
        }

        private async UniTask TryBoardVehicleFromWaitingAsync(Passenger passenger, int waitingIndex)
        {
            // Đảm bảo xe đã dừng hẳn
            if (vehicleManager.IsVehiclesMoving()) return;

            int seatIndex = GetEmptySeatIndex(passenger, vehicleManager.GetCurrentVehicle());

            if (seatIndex == -1)
            {
                await CheckLevelFailedFullWaiting();
                return;
            }

            if (!IsPassengerTagWaiting(passenger)) return;

            BoardPassenger(passenger, vehicleManager.GetCurrentVehicle(), seatIndex);
            _waitingPassengers[waitingIndex] = null;

            await MovePassengerToVehicleAsync(passenger, vehicleManager.GetCurrentVehicle());

            CompleteBoarding(passenger, vehicleManager.GetCurrentVehicle(), seatIndex);

            NextVehicleCheck(vehicleManager.GetCurrentVehicle());
        }

        private async UniTask MoveToWaitingAndCheckFailAsync(Passenger passenger,
            int row, int col, IReadOnlyList<Vector2Int> path,
            int waitingIndex, Vector3 waitingPosition)
        {
            if (!await TryMovePassengerToWaitingAsync(passenger, row, col, path,
                    waitingIndex, waitingPosition)) return;

            await CheckLevelFailedFullWaiting();
        }

        private async UniTask BoardVehicleDirectlyAsync(Passenger passenger,
            int row, int col, IReadOnlyList<Vector2Int> path,
            int seatIndex)
        {
            BoardPassenger(passenger, vehicleManager.GetCurrentVehicle(), seatIndex);

            gridManager.MarkCellEmpty(row, col);
            SetPassengerTagMoving(passenger);

            await MovePassengerAlongPathAsync(passenger, path);

            SetPassengerTagWaiting(passenger);

            if (!IsPassengerTagWaiting(passenger)) return;

            await MovePassengerToVehicleAsync(passenger, vehicleManager.GetCurrentVehicle());

            CompleteBoarding(passenger, vehicleManager.GetCurrentVehicle(), seatIndex);

            NextVehicleCheck(vehicleManager.GetCurrentVehicle());
        }

        private void BoardPassenger(Passenger passenger, Vehicle vehicle, int seatIndex)
        {
            vehicle.data.occupied[seatIndex] = true;
            _reversedPassengers++;
        }

        private void CompleteBoarding(Passenger passenger, Vehicle vehicle, int seatIndex)
        {
            _onVehiclePassengers++;
            passenger.transform.SetParent(vehicle.GetSeatTransformAtIndex(seatIndex));
            passenger.transform.localPosition = Vector3.zero;
            SetPassengerTagSitting(passenger);
        }
#endregion

#region Passenger Movement
        private async UniTask MovePassengerAlongPathAsync(Passenger passenger, IReadOnlyList<Vector2Int> path)
        {
            passenger.SetRunningAnimation(true);
            foreach (Vector2Int step in path)
            {
                Vector3 worldPos = Utilities.GridToWorldXZNeg(_columns, step.x, step.y,
                    Constants.CELL_DISTANCE, _cellContainer.position);
                float duration = GetMoveDuration(passenger.transform.position, worldPos,
                    Constants.PASSENGER_MOVE_SPEED);
                await passenger.MoveTo(worldPos, duration);
            }
            passenger.SetRunningAnimation(false);
        }

        private async UniTask MovePassengerToWaitingAreaAsync(Passenger passenger, Vector3 waitingPosition)
        {
            passenger.SetRunningAnimation(true);
            float duration = GetMoveDuration(passenger.transform.position, waitingPosition,
                Constants.PASSENGER_MOVE_SPEED);
            await passenger.MoveTo(waitingPosition, duration);
            passenger.SetRunningAnimation(false);
        }

        private async UniTask MovePassengerToVehicleAsync(Passenger passenger, Vehicle vehicle)
        {
            passenger.SetRunningAnimation(true);
            Vector3 doorPos = vehicle.GetDoorTransform().position;
            Vector3 destination = new(doorPos.x, passenger.transform.position.y, doorPos.z);
            float duration = GetMoveDuration(passenger.transform.position, destination, Constants.PASSENGER_MOVE_SPEED);
            await passenger.MoveTo(destination, duration);
            passenger.TriggerSittingAnimation();
        }
#endregion

#region Vehicle Events
        private void OnVehicleArrived(Vehicle v)
        {
            _ = HandleVehicleArrivalAsync(v);
        }

        private async UniTask HandleVehicleArrivalAsync(Vehicle vehicle)
        {
            ResetBoardingCounters();
            _ = CheckLevelFailedFullWaiting();
            List<UniTask> boardingTasks = CreateBoardingTasks(vehicle);
            await UniTask.WhenAll(boardingTasks);
            NextVehicleCheck(vehicle);
        }

        private void ResetBoardingCounters()
        {
            _reversedPassengers = 0;
            _onVehiclePassengers = 0;
        }

        private List<UniTask> CreateBoardingTasks(Vehicle vehicle)
        {
            List<UniTask> tasks = new();
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                Passenger passenger = _waitingPassengers[i];
                if (passenger == null) continue;

                int seatIndex = GetEmptySeatIndex(passenger, vehicle);
                if (seatIndex == -1) continue;

                if (!IsPassengerTagWaiting(passenger)) continue;

                int waitingIndex = i;
                tasks.Add(CreateSingleBoardingTask(passenger, vehicle, seatIndex, waitingIndex));
            }
            return tasks;
        }

        private async UniTask CreateSingleBoardingTask(Passenger passenger, Vehicle vehicle, int seatIndex, int waitingIndex)
        {
            _waitingPassengers[waitingIndex] = null;
            BoardPassenger(passenger, vehicle, seatIndex);
            await MovePassengerToVehicleAsync(passenger, vehicle);
            CompleteBoarding(passenger, vehicle, seatIndex);
        }

        private void NextVehicleCheck(Vehicle vehicle)
        {
            if (!ShouldMoveToNextVehicle(vehicle)) return;
            vehicleManager.NextVehicle();
        }
#endregion

#region Level End Conditions
        private async UniTask CheckLevelFailedFullWaiting()
        {
            if (_isLevelEnded) return;
            if (vehicleManager.GetCurrentVehicle() == null) return;
            if (!IsWaitingFull() || vehicleManager.IsVehiclesMoving()) return;

            await UniTask.WaitForSeconds(Constants.FAILED_TIME_CHECK);

            // Kiểm tra lại các điều kiện sau khi chờ
            if (_isLevelEnded) return;
            if (vehicleManager.GetCurrentVehicle() == null) return;
            if (!IsWaitingFull() || vehicleManager.IsVehiclesMoving()) return;

            // Kiểm tra xem có passenger nào đang di chuyển đến xe không
            bool hasMovingPassenger = false;
            foreach (var passenger in _waitingPassengers)
            {
                if (passenger != null && IsPassengerTagMoving(passenger))
                {
                    hasMovingPassenger = true;
                    break;
                }
            }

            if (hasMovingPassenger) return; // Còn passenger đang di chuyển, chưa fail

            if (!HasValidPassengerForVehicle(vehicleManager.GetCurrentVehicle()))
            {
                OnLevelFailed();
            }
        }
#endregion
    }
}*/

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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

        private int _reversedPassengers = 0;
        private int _onVehiclePassengers = 0;

        private int _rows;
        private int _columns;
        private int _waitAreaSize;

        private bool _isLevelInit = false;
        private bool _isLevelEnded = false;

        private int _frameCount = 0;
        private float _fpsTimer = 0f;

        private Sequence _failCheckSequence;
        
        private void Awake()
        {
            instance = this;
            vehicleManager.OnVehicleArrived += OnVehicleArrived;
            vehicleManager.OnAllVehicleDone += OnLevelComplete;
        }

        private void Update()
        {
            HandlePlayerInput();
            UpdateFPS();
        }

        private void OnDestroy()
        {
            vehicleManager.OnVehicleArrived -= OnVehicleArrived;
            vehicleManager.OnAllVehicleDone -= OnLevelComplete;
        }

        public async UniTask InitLevel(LevelData levelData)
        {
            // Initialize level data
            _rows = levelData.rows;
            _columns = levelData.columns;
            _waitAreaSize = levelData.waitAreaSize;
            
            // Initialize waiting queue
            for (int i = 0; i < _waitAreaSize; i++)
            {
                _waitingPassengers.Add(null);
            }
            
            // Load level data
            await UniTask.WhenAll(
                gridManager.LoadCellFromLevelAsync(levelData),
                gridManager.LoadWaitingTileAsync(levelData),
                passengerManager.LoadPassengerFromLevelAsync(levelData),
                vehicleManager.LoadVehicleFromLevelAsync(levelData)
            );
            _isLevelInit = true;
        }
        
        private void HandlePlayerInput()
        {
            if (!CanProcessInput()) return;
            if (!DetectClick()) return;
            ProcessPassengerClick();
        }

        private void ProcessPassengerClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int passengerLayer = LayerMask.GetMask(Constants.LAYER_NAME_PASSENGER);

            if (!Physics.Raycast(ray, out RaycastHit hit, Constants.MAX_RAYCAST_DISTANCE, passengerLayer))
                return;

            Passenger passenger = hit.collider.GetComponent<Passenger>();
            if (passenger == null) return;

            if (IsPassengerBusy(passenger)) return;

            var gridPos = passengerManager.GetGridPositionOfPassenger(passenger);
            if (gridPos.row == -1 || gridPos.column == -1)
            {
                Debug.LogWarning($"Can't find grid position of Passenger {passenger}");
                return;
            }
            HandlePassengerSelection(gridPos.row, gridPos.column);
        }

        private void HandlePassengerSelection(int row, int col)
        {
            // Kiểm tra đường đi và lấy thông tin path và passenger
            if (!TryGetPathAndPassenger(row, col, out var path, out var passenger))
            {
                passenger.Shake();
                return;
            }

            // Tìm vị trí hàng chờ trống gần nhất
            Vector3 endFirstRowPosition = CalculateEndPosition(path);
            (int waitingIndex, Vector3 waitingPosition) = GetNearestEmptyWaiting(endFirstRowPosition);

            if (waitingIndex == -1)
            {
                passenger.Shake();
                return;
            }
            
            Vehicle currentVehicle = vehicleManager.GetCurrentVehicle();
            if (currentVehicle == null) return;
            
            if (vehicleManager.IsVehiclesMoving())
            {
                _waitingPassengers[waitingIndex] = passenger;
                gridManager.MarkCellEmpty(row, col);
                passengerManager.SetStateOfPassenger(passenger, PassengerManager.ePassengerState.Moving);

                MovePassengerAlongPath(passenger, path).onComplete += () =>
                {
                    MovePassengerToPosition(passenger, waitingPosition).onComplete += () =>
                    {
                        passengerManager.SetStateOfPassenger(passenger, PassengerManager.ePassengerState.Waiting);
                        
                        // Nếu xe vẫn đang di chuyển thì passenger dừng
                        if (vehicleManager.IsVehiclesMoving()) return;
                        
                        // Còn nếu xe không di chuyển, kiểm tra xem có add đc vào xe ko
                        int seatIndex = GetEmptySeatIndex(passenger, vehicleManager.GetCurrentVehicle());
                        if (seatIndex == -1)
                        {
                            // Không add được vào xe, kiểm tra điều kiện thua
                            Debug.Log("01");
                        }
                        else
                        {
                            Debug.Log("02");
                            // // Add lên xe
                            // if (!IsPassengerTagWaiting(passenger)) return;
                            //
                            // vehicleManager.GetCurrentVehicle().data.occupied[seatIndex] = true;
                            // _reversedPassengers++;
                            // _waitingPassengers[waitingIndex] = null;
                            //
                            // await MovePassengerToVehicleAsync(passenger, vehicleManager.GetCurrentVehicle());
                            //
                            // _onVehiclePassengers++;
                            // passenger.transform.SetParent(vehicleManager.GetCurrentVehicle().GetSeatTransformAtIndex(seatIndex));
                            // passenger.transform.localPosition = Vector3.zero;
                            // SetPassengerTagSitting(passenger);
                            //
                            // await NextVehicleCheckAsync(vehicleManager.GetCurrentVehicle());
                        }
                    };
                };
            }
            else
            {
                int seatIndex = GetEmptySeatIndex(passenger, vehicleManager.GetCurrentVehicle());
                if (seatIndex == -1)
                {
                    _waitingPassengers[waitingIndex] = passenger;
                    gridManager.MarkCellEmpty(row, col);
                    passengerManager.SetStateOfPassenger(passenger, PassengerManager.ePassengerState.Moving);

                    MovePassengerAlongPath(passenger, path).onComplete += () =>
                    {
                        MovePassengerToPosition(passenger, waitingPosition).onComplete += () =>
                        {
                            passengerManager.SetStateOfPassenger(passenger, PassengerManager.ePassengerState.Waiting);
                            
                            // Check dieu kien thua
                            Debug.Log("03");
                        };
                    };
                }
                else
                {
                    vehicleManager.GetCurrentVehicle().data.occupied[seatIndex] = true;
                    _reversedPassengers++;
                    
                    gridManager.MarkCellEmpty(row, col);
                    passengerManager.SetStateOfPassenger(passenger, PassengerManager.ePassengerState.Moving);

                    MovePassengerAlongPath(passenger, path).onComplete += () =>
                    {
                        passengerManager.SetStateOfPassenger(passenger, PassengerManager.ePassengerState.Waiting);
                        
                        if (passengerManager.GetStateOfPassenger(passenger) != PassengerManager.ePassengerState.Waiting) return;
                        
                        Debug.Log("04");
                        
                        Vehicle vehicle = vehicleManager.GetCurrentVehicle();
                        MovePassengerToPosition(passenger, vehicle.GetDoorTransform().position, true).onComplete += () =>
                        {
                            _onVehiclePassengers++;
                            passenger.transform.SetParent(vehicle.GetSeatTransformAtIndex(seatIndex));
                            passenger.transform.localPosition = Vector3.zero;
                            passengerManager.SetStateOfPassenger(passenger, PassengerManager.ePassengerState.Sitting);
                            
                            Debug.Log("04-1");
                            // Check next vehicle
                            
                        };

                        // await MovePassengerToVehicleAsync(passenger, vehicleManager.GetCurrentVehicle());
                        //
                        // _onVehiclePassengers++;
                        // p.transform.SetParent(vehicleManager.GetCurrentVehicle().GetSeatTransformAtIndex(seatIndex));
                        // p.transform.localPosition = Vector3.zero;
                        // SetPassengerTagSitting(p);
                        //
                        // await NextVehicleCheckAsync(vehicleManager.GetCurrentVehicle());
                    };
                }
            }
        }

        private Sequence MovePassengerAlongPath(Passenger passenger, IReadOnlyList<Vector2Int> path)
        {
            passenger.SetRunningAnimation(true);
            
            Sequence sequence = DOTween.Sequence();
            sequence.onComplete += () => passenger.SetRunningAnimation(false);
            sequence.SetAutoKill(true);
            foreach (Vector2Int step in path)
            {
                Vector3 worldPos = CalculateWorldPosition(step.x, step.y);
                sequence.Append(MovePassengerToPosition(passenger, worldPos));
            }
            
            return sequence;
        }

        private Tween MovePassengerToPosition(Passenger passenger, Vector3 worldPosition, bool triggerSittingAnimation = false)
        {
            passenger.SetRunningAnimation(true);
            
            float duration = GetMoveDuration(passenger.transform.position, worldPosition, Constants.PASSENGER_MOVE_SPEED);
            
            Tween moveTween = passenger.MoveTo(worldPosition, duration);
            moveTween.onComplete += () =>
            {
                if (triggerSittingAnimation) passenger.TriggerSittingAnimation();
                else passenger.SetRunningAnimation(false);
            };
            moveTween.SetAutoKill(true);
            
            return moveTween;
        }
        
        private void OnVehicleArrived(Vehicle vehicle)
        {
            
        }
        
        private void OnLevelComplete()
        {
            if (_isLevelEnded) return;
            _isLevelEnded = true;
            Debug.Log("Level completed");
            _tempTmpText.text = "Level Completed";
        }

        private void OnLevelFailed()
        {
            if (_isLevelEnded) return;
            _isLevelEnded = true;
            Debug.Log("Level failed");
            _tempTmpText.text = "Level Failed";
        }
        
        private bool CanProcessInput()
        {
            return _isLevelInit && !_isLevelEnded;
        }

        private bool DetectClick()
        {
            // PC or Editor
            if (Input.GetMouseButtonDown(0)) return true;

            // Mobile
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended) return true;
            }

            return false;
        }
        
        private void UpdateFPS()
        {
            _frameCount++;
            _fpsTimer += Time.deltaTime;

            if (!(_fpsTimer >= 1f)) return;

            int fps = Mathf.RoundToInt(_frameCount / _fpsTimer);

            _fpsTmpText.text = $"FPS: {fps}";

            _frameCount = 0;
            _fpsTimer = 0f;
        }
        
        private bool ShouldMoveToNextVehicle(Vehicle vehicle)
        {
            return IsVehicleFull(vehicle) && _reversedPassengers >= Constants.VEHICLE_SEAT_SLOTS && _onVehiclePassengers >= Constants.VEHICLE_SEAT_SLOTS;
        }

        private bool IsVehicleFull(Vehicle vehicle)
        {
            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                if (!vehicle.data.occupied[i]) return false;
            }
            return true;
        }

        private int GetEmptySeatIndex(Passenger passenger, Vehicle vehicle)
        {
            if (IsVehicleFull(vehicle) || passenger == null || vehicle == null) return -1;

            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                if (vehicle.data.occupied[i]) continue;
                if (vehicle.data.colorType == passenger.data.colorType) return i;
            }

            return -1;
        }

        private bool HasValidPassengerForVehicle(Vehicle vehicle)
        {
            foreach (var passenger in _waitingPassengers)
            {
                if (passenger == null) continue;
                if (GetEmptySeatIndex(passenger, vehicle) != -1) return true;
            }
            return false;
        }

        private bool IsPassengerBusy(Passenger passenger)
        {
            PassengerManager.ePassengerState state = passengerManager.GetStateOfPassenger(passenger);
            return state is not PassengerManager.ePassengerState.Idle;
        }

        private bool IsWaitingFull()
        {
            foreach (Passenger passenger in _waitingPassengers)
            {
                if (passenger == null) return false;
            }
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
                if (!tilePos.HasValue) continue;

                float distance = Vector3.Distance(tilePos.Value, fromPosition);
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    nearestIndex = i;
                    nearestPosition = tilePos.Value;
                }
            }

            return nearestIndex == -1 ? (-1, Vector3.zero) : (nearestIndex, nearestPosition);
        }
        
        private float GetMoveDuration(Vector3 from, Vector3 to, float speed)
        {
            return Vector3.Distance(from, to) / speed;
        }
        
        private bool TryGetPathAndPassenger(int r, int c, out IReadOnlyList<Vector2Int> path, out Passenger passenger)
        {
            path = gridManager.GetPathToFirstRow(r, c);
            passenger = passengerManager.GetPassengerAtGridPosition(r, c);
            return (path != null);
        }

        private Vector3 CalculateEndPosition(IReadOnlyList<Vector2Int> path)
        {
            if (path.Count == 0) return CalculateWorldPosition(0, 0);
            Vector2Int lastPoint = path[^1];
            return CalculateWorldPosition(lastPoint.x, lastPoint.y);
        }
        
        private Vector3 CalculateWorldPosition(int row, int col)
        {
            return Utilities.GridToWorldXZNeg(
                _columns, 
                row, 
                col, 
                Constants.CELL_DISTANCE, 
                _cellContainer.position
            );
        }
    }
}