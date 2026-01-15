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

        [SerializeField] private TMP_Text _levelStatusTmpText;
        [SerializeField] private TMP_Text _fpsTmpText;
        
        private Camera _mainCamera;
        private readonly List<Passenger> _waitingPassengers = new();
        private readonly Dictionary<Passenger, MoveDataEntry> _passengerMoveData = new();
        
        private LevelData _loadedLevelData;
        private bool _isInstanceInit = false;
        private bool _isWaitForInstanceInit = false;
        private bool _isLevelInit = false;
        private bool _isLevelEnded = false;

        private int _frameCount = 0;
        private float _fpsTimer = 0f;
        
        private int _passengerLayerMask;
        private readonly Vector2Int[] _checkDirections = {
            new(1, 0),   // down
            new(0, -1),  // left
            new(-1, 0),  // up
            new(0, 1)    // right
        };

        private class MoveDataEntry
        {
            public int fromRow = -1;
            public int fromColumn = -1;
            public IReadOnlyList<Vector2Int> pathToFirstRow;
            public int toWaitingIndex = -1;
            public Vector3 toWaitingPosition;
            public int preservedSeatIndex = -1;
        }

        private void Awake()
        {
            instance = this;
            _isInstanceInit = true;
            
            GridManager.CellMarkedEmpty += OnCellMarkedEmpty;
            GridManager.CellMarkedOccupied += OnCellMarkedOccupied;
            GridManager.NewWaitingTileAdded += OnNewWaitingTileAdded;
            Passenger.PassengerStateChanged += OnPassengerStateChanged;
            VehicleManager.VehicleIndexArrived += OnVehicleIndexArrived;
            VehicleManager.LastVehicleLeft += OnLastVehicleLeft;
            
            _mainCamera = Camera.main;
            _passengerLayerMask = LayerMask.GetMask(Constants.LAYER_NAME_PASSENGER);
        }

        private void Update()
        {
            ProcessPassengerClick();
            UpdateFPSText();
        }

        private void UpdateFPSText()
        {
            _frameCount++;
            _fpsTimer += Time.deltaTime;
            if (_fpsTimer < 1f) return;
            _fpsTimer -= 1f;
            _fpsTmpText.text = $"FPS: {_frameCount}";
            _frameCount = 0;
        }

        private void OnDestroy()
        {
            GridManager.CellMarkedEmpty -= OnCellMarkedEmpty;
            GridManager.CellMarkedOccupied -= OnCellMarkedOccupied;
            GridManager.NewWaitingTileAdded -= OnNewWaitingTileAdded;
            Passenger.PassengerStateChanged -= OnPassengerStateChanged;
            VehicleManager.VehicleIndexArrived -= OnVehicleIndexArrived;
            VehicleManager.LastVehicleLeft -= OnLastVehicleLeft;
            
            if (instance == this) instance = null;
        }

        public async UniTask InitLevel(LevelData levelData)
        {
            if (!await WaitForInstanceInit()) return;
            
            _loadedLevelData = levelData;
            
            // Init waiting passengers
            for (int i = 0; i < _loadedLevelData.waitAreaSize; i++) _waitingPassengers.Add(null);
            
            // Load level components and data
            await UniTask.WhenAll
            (
                GridManager.instance.LoadCellFromLevelAsync(_loadedLevelData),
                GridManager.instance.LoadWaitingTileAsync(_loadedLevelData),
                PassengerManager.instance.LoadPassengerFromLevelAsync(_loadedLevelData),
                VehicleManager.instance.LoadVehicleFromLevelAsync(_loadedLevelData)
            );
            _isLevelInit = true;
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
        
        private void ProcessPassengerClick()
        {
            if (!CanProcessInput() || !DetectClick()) return;
            
            if (TryGetClickedPassenger(out Passenger passenger))
            {
                HandlePassengerSelection(passenger);
            }
        }
        
        private bool CanProcessInput()
        {
            return _isInstanceInit && _isLevelInit && !_isLevelEnded;
        }

        private bool DetectClick()
        {
            // // PC or Editor
            // if (Input.GetMouseButtonDown(0)) return true;
            //
            // // Mobile
            // if (Input.touchCount > 0)
            // { 
            //     Touch touch = Input.GetTouch(0); 
            //     if (touch.phase == TouchPhase.Ended) return true;
            // } 
            // return false;
            
            // Same as above
            #if UNITY_EDITOR || UNITY_STANDALONE
            return Input.GetMouseButtonDown(0);
            #else
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
            #endif
        }

        private bool TryGetClickedPassenger(out Passenger passenger)
        {
            passenger = null;
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (!Physics.Raycast(ray, out RaycastHit hit, Constants.MAX_RAYCAST_DISTANCE, _passengerLayerMask)) return false;
            
            passenger = hit.collider.GetComponent<Passenger>();
            return passenger != null;
        }

        private void HandlePassengerSelection(Passenger passenger)
        {
            if (passenger.state is not ePassengerState.Idle)
            {
                Debug.Log("That passenger is not in idle state");
                return;
            }

            if (!TryHandleSpecialPassenger(passenger)) return;
            if (!TryGetPassengerGridPosition(passenger, out int row, out int column)) return;
            if (!TryGetPathFromGridPosition(row, column, out var pathToFirstRow))
            {
                passenger.Shake(); 
                return;
            }
            
            if (!TryGetWaitingPosition(pathToFirstRow, out int waitingIndex, out Vector3 waitingPosition))
            {
                passenger.Shake();
                return;
            }

            // Initiate passenger move
            if (!_passengerMoveData.TryGetValue(passenger, out var moveDataRef))
            {
                moveDataRef = new MoveDataEntry();
                _passengerMoveData[passenger] = moveDataRef;
            }
            
            Vehicle currentVehicle = VehicleManager.instance.GetCurrentVehicle();
            int seatIndex = VehicleManager.instance.IsVehiclesMoving() ? -1 : GetEmptySeatIndexForPassenger(passenger, currentVehicle);
            
            moveDataRef.fromRow = row;
            moveDataRef.fromColumn = column;
            moveDataRef.pathToFirstRow = pathToFirstRow;
            moveDataRef.toWaitingIndex = waitingIndex;
            moveDataRef.toWaitingPosition = waitingPosition;
            moveDataRef.preservedSeatIndex = seatIndex;
            
            passenger.SetState(ePassengerState.MovingToFirstRow);
        }

        private bool TryHandleSpecialPassenger(Passenger passenger)
        {
            switch (passenger)
            {
                case CloakPassenger { data: { passengerType: ePassengerType.Cloak, extraData: CloakPassengerData cloakData } } cloakPassenger:
                {
                    if (!cloakData.isRevealed)
                    {
                        cloakPassenger.Shake(); 
                        return false;
                    }
                    cloakPassenger.DropCloak();
                    break;
                }
                case RopePassenger { data: { passengerType: ePassengerType.Rope, extraData: RopePassengerData ropeData } } ropePassenger:
                {
                    if (ropeData.ropeCount > 0)
                    {
                        ropePassenger.Shake(); 
                        return false;
                    }
                    break;
                }
                case BombPassenger { data: { passengerType: ePassengerType.Bomb, extraData: BombPassengerData bombData } } bombPassenger:
                {
                    if (bombData.bombTime > 0) bombPassenger.ToggleBomb(false);
                    else Debug.LogWarning("That passenger must be exploded before moving");
                    break;
                }
            }
            return true;
        }

        private bool TryGetPassengerGridPosition(Passenger passenger, out int row, out int column)
        {
            (row, column) = PassengerManager.instance.GetGridPositionOfPassenger(passenger);
            if (row == -1 || column == -1)
            {
                Debug.LogError($"Can't find grid position of Passenger {passenger}");
                return false;
            }
            return true;
        }
        
        private bool TryGetPathFromGridPosition(int r, int c, out IReadOnlyList<Vector2Int> path)
        {
            path = GridManager.instance.GetPathToFirstRow(r, c);
            return (path != null);
        }

        private bool TryGetWaitingPosition(IReadOnlyList<Vector2Int> pathToFirstRow, out int waitingIndex, out Vector3 waitingPosition)
        {
            Vector3 endFirstRowPosition = GridManager.instance.GetEndPositionOfPath(pathToFirstRow);
            (waitingIndex, waitingPosition) = GetNearestEmptyWaiting(endFirstRowPosition);
            return waitingIndex != -1;
        }
        
        private void OnPassengerStateChanged(Passenger passenger, ePassengerState fromState, ePassengerState toState)
        {
            // Handle some special cases
            switch (toState)
            {
                case ePassengerState.Inactive:
                {
                    passenger.gameObject.SetActive(false);
                    _passengerMoveData.Remove(passenger);
                    return;
                }
                case ePassengerState.None:
                {
                    Debug.LogWarning($"Passenger {passenger.name} is not spawned correctly (From {fromState} to {toState})");
                    return;
                }
            }
            
            if (!_passengerMoveData.TryGetValue(passenger, out var moveDataRef))
            {
                Debug.LogWarning("Can't find move data of passenger");
                return;
            }
            
            Vehicle currentVehicle = VehicleManager.instance.GetCurrentVehicle();
            if (currentVehicle == null)
            {
                Debug.LogWarning("Can't find current vehicle or level is ended");
                return;
            }
            
            // Handle passenger state changes
            switch (fromState)
            {
                // Moving to first row
                case ePassengerState.Idle when (toState is ePassengerState.MovingToFirstRow):
                {
                    // Mark cell empty because passenger is moving to first row
                    GridManager.instance.MarkCellEmpty(moveDataRef.fromRow, moveDataRef.fromColumn);
                    
                    // Preserve seat index if available
                    if (moveDataRef.preservedSeatIndex == -1) _waitingPassengers[moveDataRef.toWaitingIndex] = passenger;
                    else currentVehicle.SetReservedPassenger(moveDataRef.preservedSeatIndex, passenger);

                    // Build movement path to first row
                    Vector3[] path = new Vector3[moveDataRef.pathToFirstRow.Count];
                    for (int i = 0; i < moveDataRef.pathToFirstRow.Count; i++)
                    {
                        Vector2Int point = moveDataRef.pathToFirstRow[i];
                        Vector3 pathPosition = GridManager.instance.GetWorldPositionOfGridPosition(point.x, point.y);
                        pathPosition.y = passenger.transform.position.y;
                        path[i] = pathPosition;
                    }

                    // Move passenger to first row
                    passenger.SetRunningAnimation(true);
                    passenger.MovePath(path, Constants.PASSENGER_MOVE_SPEED).onComplete += () =>
                    {
                        // On complete, change passenger state to first row
                        passenger.SetState(ePassengerState.FirstRow);
                    };
                    break;
                }
                // Moved to first row (on the first row)
                case ePassengerState.MovingToFirstRow when (toState is ePassengerState.FirstRow):
                {
                    // Change state based on preserved seat index
                    ePassengerState nextState = moveDataRef.preservedSeatIndex == -1 
                        ? ePassengerState.MovingToQueue 
                        : ePassengerState.MovingToVehicle;
                    passenger.SetState(nextState);
                    break;
                }
                // Moving to waiting position
                case ePassengerState.FirstRow when (toState is ePassengerState.MovingToQueue):
                {
                    // Move passenger to waiting position
                    Vector3 waitingPosition = moveDataRef.toWaitingPosition;
                    passenger.MoveTo(waitingPosition, Constants.PASSENGER_MOVE_SPEED).onComplete += () =>
                    {
                        // On complete, change passenger state to waiting
                        passenger.SetState(ePassengerState.Waiting);
                    };
                    break;
                }
                // Moved to waiting position (on waiting queue)
                case ePassengerState.MovingToQueue when (toState is ePassengerState.Waiting):
                {
                    // Check if passenger can board vehicle
                    bool canMoveToVehicle = CheckForPassengerMoveToVehicle(passenger, currentVehicle);
                    passenger.SetRunningAnimation(canMoveToVehicle);
                    
                    // If can't board vehicle, check for level failed
                    if (!canMoveToVehicle)
                    {
                        if (!HasAnyValidMove() && !HasEmptyWaitingSlot())
                        {
                            LevelFailedHandle(0);
                        }
                    }
                    break;
                }
                // Moving to vehicle (direct boarding)
                case ePassengerState.FirstRow when (toState is ePassengerState.MovingToVehicle):
                {
                    // Move passenger to vehicle door
                    Vector3 doorPosition = currentVehicle.GetDoorTransform().position;
                    passenger.MoveTo(doorPosition, Constants.PASSENGER_MOVE_SPEED).onComplete += () =>
                    {
                        // On complete, change passenger state to sitting on vehicle
                        passenger.SetState(ePassengerState.Sitting);
                    };
                    break;
                }
                // Moving to vehicle (waiting boarding)
                case ePassengerState.Waiting when (toState is ePassengerState.MovingToVehicle):
                {
                    if (VehicleManager.instance.IsVehiclesMoving()) return;
                    
                    // Preserved seat index for passenger and remove from waiting queue
                    _waitingPassengers[moveDataRef.toWaitingIndex] = null;
                    currentVehicle.SetReservedPassenger(moveDataRef.preservedSeatIndex, passenger);
                    
                    // Move passenger to vehicle door
                    Vector3 doorPosition = currentVehicle.GetDoorTransform().position;
                    passenger.SetRunningAnimation(true);
                    passenger.MoveTo(doorPosition, Constants.PASSENGER_MOVE_SPEED).onComplete += () =>
                    {
                        // On complete, change passenger state to sitting on vehicle
                        passenger.SetState(ePassengerState.Sitting);
                    };
                    break;
                }
                // Moved to vehicle (sitting on the vehicle)
                case ePassengerState.MovingToVehicle when (toState is ePassengerState.Sitting):
                {
                    // Set passenger position to seat position
                    Transform seatTransform = currentVehicle.GetSeatTransformAtIndex(moveDataRef.preservedSeatIndex);
                    passenger.transform.SetPositionAndRotation(seatTransform.position, Quaternion.identity);
                    passenger.transform.SetParent(seatTransform);
                    passenger.TriggerSittingAnimation();
                    
                    // Set seat occupied flag
                    currentVehicle.SetSeatOccupied(moveDataRef.preservedSeatIndex, true);
                    
                    // Check for next vehicle
                    if (VehicleManager.instance.IsVehicleFull(currentVehicle))
                    {
                        CheckForNextVehicle(currentVehicle);
                    }
                    break;
                }
                default:
                {
                    Debug.Log($"Unhandled passenger state change: \"{passenger.name}\" from \"{fromState}\" to \"{toState}\"");
                    break;
                }
            }
        }

        private void OnVehicleIndexArrived(int vehicleIndex)
        {
            Vehicle vehicle = VehicleManager.instance.GetVehicleAtIndex(vehicleIndex);
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                Passenger passenger = _waitingPassengers[i];
                if (passenger == null) continue;
                if (passenger.state is not ePassengerState.Waiting) continue;
                passenger.SetRunningAnimation(CheckForPassengerMoveToVehicle(passenger, vehicle));
            }
            if (!HasAnyValidMove() && !HasEmptyWaitingSlot())
            {
                LevelFailedHandle(1);
            }
        }

        private bool CheckForPassengerMoveToVehicle(Passenger passenger, Vehicle vehicle)
        {
            if (passenger == null || !_passengerMoveData.TryGetValue(passenger, out var moveDataRef))
                return false;
            
            int seatIndex = GetEmptySeatIndexForPassenger(passenger, vehicle);
            if (seatIndex == -1) return false;
            
            moveDataRef.preservedSeatIndex = seatIndex;
            vehicle.SetReservedPassenger(seatIndex, passenger);
            passenger.SetState(ePassengerState.MovingToVehicle);
            return true;
        }
        
        private void OnLastVehicleLeft()
        {
            LevelCompletedHandle();
        }

        private void OnCellMarkedEmpty(Cell cell, int row, int column)
        {
            for (int dir_index = 0; dir_index < _checkDirections.Length; dir_index++)
            {
                Vector2Int direction = _checkDirections[dir_index];
                (int nextRow, int nextColumn) = (row + direction.x, column + direction.y);
                if (!IsValidGridPosition(nextRow, nextColumn)) continue;
                
                Cell nextCell = GridManager.instance.GetCellAtGridPosition(nextRow, nextColumn);
                if (nextCell == null) continue;
                
                // Handle cell types
                switch (nextCell)
                {
                    case TunnelCell { data: { cellType: eCellType.Tunnel, extraData: TunnelCellData tunnelData } } tunnelCell:
                    {
                        // Có nghĩa là khi hướng check là hướng dưới (dir index 0)
                        // mà hướng Tunnel ra là hướng trên (direction 0)
                        // thì sẽ cho passenger ra kho hầm 
                        if (dir_index == tunnelData.direction)
                        {
                            // Move first passenger out of tunnel
                            if (tunnelCell.passengers.Count > 0)
                            {
                                Passenger tunnelPassenger = tunnelCell.passengers[0];
                                Vector3 worldPos = GridManager.instance.GetWorldPositionOfGridPosition(row, column);
                                PassengerManager.instance.SetPassengerAtGridPosition(row, column, tunnelPassenger);
                                GridManager.instance.MarkCellOccupied(row, column);
                                
                                tunnelPassenger.gameObject.SetActive(true);
                                tunnelPassenger.transform.DOScale(Vector3.one, Constants.PASSENGER_OUT_TUNNEL_DURATION).SetAutoKill();
                                tunnelPassenger.SetRunningAnimation(true);
                                tunnelPassenger.MoveTo(worldPos, Constants.PASSENGER_MOVE_SPEED).onComplete += () =>
                                {
                                    tunnelPassenger.SetRunningAnimation(false);
                                };
                                
                                tunnelCell.passengers.RemoveAt(0);
                                tunnelCell.SetTunnelCount(tunnelCell.passengers.Count);
                            }
                        }
                        break;
                    }
                }
                
                Passenger nextPassenger = PassengerManager.instance.GetPassengerAtGridPosition(nextRow, nextColumn);
                if (nextPassenger == null) continue;
                if (nextPassenger.state is not ePassengerState.Idle) continue;
                
                // Handle passenger types
                switch (nextPassenger)
                {
                    case RopePassenger { data: { passengerType: ePassengerType.Rope, extraData: RopePassengerData ropeData } } ropePassenger:
                    {
                        if (ropeData.ropeCount > 0)
                        {
                            ropeData.ropeCount = Mathf.Max(0, ropeData.ropeCount - 1);
                            ropePassenger.SetRopeCount(ropeData.ropeCount);
                        }
                        break;
                    }
                }
            }
            UpdateHiddenPassengers();
            UpdateCloakPassengers();
            UpdateBombPassengers();
        }

        private void UpdateHiddenPassengers()
        {
            List<Passenger> hiddenPassengers = PassengerManager.instance.GetPassengersByType(ePassengerType.Hidden);
            if (hiddenPassengers == null) return;
            foreach (Passenger passenger in hiddenPassengers)
            {
                if (passenger == null) continue;
                if (passenger.state is not ePassengerState.Idle) continue;
                    
                (int r, int c) = PassengerManager.instance.GetGridPositionOfPassenger(passenger);
                if (!TryGetPathFromGridPosition(r, c, out var path)) continue;
                    
                HiddenPassenger hiddenPassenger = (HiddenPassenger) passenger;
                if (path.Count == 0)
                {
                    hiddenPassenger.SetRevealedImmediately();
                    continue;
                }
                
                // If has path then reveal passenger
                if (!hiddenPassenger.IsRevealed())
                {
                    Color toColor = GameManager.GetColorByType(hiddenPassenger.data.colorType);
                    hiddenPassenger.Reveal(toColor);
                }
            }
        }

        private void UpdateCloakPassengers()
        {
            List<Passenger> cloakPassengers = PassengerManager.instance.GetPassengersByType(ePassengerType.Cloak);
            if (cloakPassengers == null) return;
            foreach (Passenger passenger in cloakPassengers)
            {
                if (passenger == null) continue;
                if (passenger.state is not ePassengerState.Idle) continue;
    
                CloakPassenger cloakPassenger = (CloakPassenger) passenger;
                CloakPassengerData cloakData = (CloakPassengerData) cloakPassenger.data.extraData;
                cloakData.isRevealed = !cloakData.isRevealed;
                _ = cloakData.isRevealed ? cloakPassenger.CloakOff() : cloakPassenger.CloakOn();
            }
        }

        private void UpdateBombPassengers()
        {
            List<Passenger> bombPassengers = PassengerManager.instance.GetPassengersByType(ePassengerType.Bomb);
            if (bombPassengers == null) return;
            foreach (Passenger passenger in bombPassengers)
            {
                if (passenger == null) continue;
                if (passenger.state is not ePassengerState.Idle) continue;
                    
                BombPassenger bombPassenger = (BombPassenger) passenger;
                BombPassengerData bombData = (BombPassengerData) bombPassenger.data.extraData;
                bombData.bombTime = Mathf.Max(0, bombData.bombTime - 1);
                bombPassenger.SetBombTime(bombData.bombTime);

                if (bombData.bombTime <= 0)
                {
                    LevelFailedHandle(2);
                }
            }
        }

        private void OnCellMarkedOccupied(Cell cell, int row, int column)
        {
            
        }

        private void OnNewWaitingTileAdded(int newCount)
        {
            if (_waitingPassengers.Count >= newCount) return;
            
            // Add a new passenger slot to waiting queue
            _waitingPassengers.Add(null);
            
            // Change valid passenger's position to match the new count
            foreach (var kvp in _passengerMoveData)
            {
                Passenger passenger = kvp.Key;
                MoveDataEntry moveData = kvp.Value;
                
                Vector3? waitingPosition = GridManager.instance.GetPositionOfWaitingTileIndex(moveData.toWaitingIndex);
                if (waitingPosition.HasValue)
                {
                    moveData.toWaitingPosition = waitingPosition.Value;
                    switch (passenger.state)
                    {
                        case ePassengerState.Waiting:
                        {
                            // Nếu đang waiting thì set vị trí luôn
                            passenger.transform.SetPositionAndRotation(moveData.toWaitingPosition, Quaternion.identity);
                            break;
                        }
                        case ePassengerState.MovingToQueue:
                        {
                            // Nếu đang di chuyển đến hàng chờ thì đổi hướng
                            passenger.MoveTo(moveData.toWaitingPosition, Constants.PASSENGER_MOVE_SPEED);
                            break;
                        }
                    }
                }
            }
        }

        private void LevelCompletedHandle()
        {
            if (_isLevelEnded) return;
            _isLevelEnded = true;
            
            DOTween.KillAll();
            _levelStatusTmpText.text = "Level completed!";
            _levelStatusTmpText.color = Color.green;
        }

        private void LevelFailedHandle(int reason)
        {
            if (_isLevelEnded) return;
            _isLevelEnded = true;

            string reasonString = "";
            switch (reason)
            {
                case 0:
                {
                    reasonString = "Waiting queue is full\nand no valid move\n";
                    break;
                }
                case 1:
                {
                    reasonString = "Waiting queue is full\nand no valid move (veh)\n";
                    break;
                }
                case 2:
                {
                    reasonString = "Bomb exploded\n";
                    break;
                }
            }
            reasonString += "Level failed!";
            
            DOTween.KillAll();
            _levelStatusTmpText.text = reasonString;
            _levelStatusTmpText.color = Color.red;
        }
        
        private bool HasAnyValidMove()
        {
            if (VehicleManager.instance.IsVehiclesMoving()) return true;
            
            Vehicle vehicle = VehicleManager.instance.GetCurrentVehicle();
            if (vehicle == null) return false;
            
            foreach (var kv in _passengerMoveData)
            {
                Passenger passenger = kv.Key;
                MoveDataEntry moveData = kv.Value;

                if (passenger == null) continue; 
                if (passenger.state == ePassengerState.Inactive) continue;

                // Đang chạy thì chưa thua
                if (passenger.state is
                    ePassengerState.MovingToFirstRow or
                    ePassengerState.FirstRow or
                    ePassengerState.MovingToQueue or
                    ePassengerState.MovingToVehicle)
                    return true;

                // Đang chờ nhưng đã giữ ghế
                if (passenger.state == ePassengerState.Waiting && moveData.preservedSeatIndex != -1)
                    return true;
            }

            return false;
        }

        private void CheckForNextVehicle(Vehicle currentVehicle)
        {
            VehicleManager.instance.NextVehicleSequence().onComplete += () =>
            {
                for (int i = 0; i < currentVehicle.reservedPassengers.Length; i++)
                {
                    Passenger passenger = currentVehicle.reservedPassengers[i];
                    if (passenger == null) continue;
                    passenger.SetState(ePassengerState.Inactive);
                }
            };
        }
        
        private (int, Vector3) GetNearestEmptyWaiting(Vector3 fromPosition)
        {
            float smallestDistance = float.MaxValue;
            int nearestIndex = -1;
            Vector3 nearestPosition = Vector3.zero;
            
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                if (_waitingPassengers[i] != null) continue;
                Vector3? tilePos = GridManager.instance.GetPositionOfWaitingTileIndex(i);
                if (!tilePos.HasValue) continue;
                float distance = Vector3.Distance(tilePos.Value, fromPosition);
                if (distance >= smallestDistance) continue;
                smallestDistance = distance;
                nearestIndex = i;
                nearestPosition = tilePos.Value;
            }
            
            if (nearestIndex == -1) return (-1, Vector3.zero);
            return (nearestIndex, nearestPosition);
        }
        
        private int GetEmptySeatIndexForPassenger(Passenger passenger, Vehicle vehicle)
        {
            if (passenger == null || vehicle == null) return -1;
            if (VehicleManager.instance.IsVehicleFull(vehicle)) return -1;

            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                if (vehicle.seatOccupied[i] || vehicle.reservedPassengers[i]) continue;
                if (vehicle.data.colorType == passenger.data.colorType) return i;
            }
            return -1;
        }

        private bool HasEmptyWaitingSlot()
        {
            foreach (var p in _waitingPassengers)
                if (p == null) return true;
            return false;
        }
        
        private bool IsValidGridPosition(int row, int col)
        {
            return Utilities.IsInBounds(_loadedLevelData.rows, _loadedLevelData.columns, row, col);
        }
    }
}