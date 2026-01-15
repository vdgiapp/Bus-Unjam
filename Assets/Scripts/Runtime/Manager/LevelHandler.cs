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

        private List<Passenger> _waitingPassengers = new();
        private Dictionary<Passenger, MoveDataEntry> _passengerMoveData = new();
        
        private LevelData _loadedLevelData;
        private bool _isInstanceInit = false;
        private bool _isWaitForInstanceInit = false;
        private bool _isLevelInit = false;
        private bool _isLevelEnded = false;

        private int _frameCount = 0;
        private float _fpsTimer = 0f;

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
        }

        private void Update()
        {
            ProcessPassengerClick();
        }

        private void OnDestroy()
        {
            GridManager.CellMarkedEmpty -= OnCellMarkedEmpty;
            GridManager.CellMarkedOccupied -= OnCellMarkedOccupied;
            GridManager.NewWaitingTileAdded -= OnNewWaitingTileAdded;
            Passenger.PassengerStateChanged -= OnPassengerStateChanged;
            VehicleManager.VehicleIndexArrived -= OnVehicleIndexArrived;
            VehicleManager.LastVehicleLeft -= OnLastVehicleLeft;
        }

        public async UniTask InitLevel(LevelData levelData)
        {
            if (!await WaitForInstanceInit()) return;
            
            _loadedLevelData = levelData;
            for (int i = 0; i < _loadedLevelData.waitAreaSize; i++)
            {
                _waitingPassengers.Add(null);
            }
            
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
            if (!CanProcessInput()) return;
            if (!DetectClick()) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int passengerLayer = LayerMask.GetMask(Constants.LAYER_NAME_PASSENGER);
            
            if (!Physics.Raycast(ray, out RaycastHit hit, Constants.MAX_RAYCAST_DISTANCE, passengerLayer)) return;
            
            Passenger passenger = hit.collider.GetComponent<Passenger>();
            if (!passenger) return;
            HandlePassengerSelection(passenger);
        }
        
        private bool CanProcessInput()
        {
            return _isInstanceInit && _isLevelInit && !_isLevelEnded;
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

        private void HandlePassengerSelection(Passenger passenger)
        {
            if (PassengerManager.instance.IsPassengerBusy(passenger))
            {
                Debug.Log("That passenger is busy");
                return;
            }

            switch (passenger)
            {
                case CloakPassenger { data: { passengerType: ePassengerType.Cloak, extraData: CloakPassengerData cloakData } } cloakPassenger:
                {
                    if (!cloakData.isRevealed)
                    {
                        cloakPassenger.Shake(); 
                        return;
                    }
                    cloakPassenger.DropCloak();
                    break;
                }
                case RopePassenger { data: { passengerType: ePassengerType.Rope, extraData: RopePassengerData ropeData } } ropePassenger:
                {
                    if (ropeData.ropeCount > 0)
                    {
                        ropePassenger.Shake(); 
                        return;
                    }
                    break;
                }
                case BombPassenger { data: { passengerType: ePassengerType.Bomb, extraData: BombPassengerData bombData } } bombPassenger:
                {
                    if (bombData.bombTime > 0)
                    {
                        bombPassenger.ToggleBomb(false);
                    }
                    else
                    {
                        Debug.LogWarning("That passenger must be exploded before moving");
                    }
                    break;
                }
            }

            if (!_passengerMoveData.TryGetValue(passenger, out var moveDataRef))
            {
                moveDataRef = new MoveDataEntry();
            }
            
            (int row, int column) = PassengerManager.instance.GetGridPositionOfPassenger(passenger);
            if (row == -1 || column == -1)
            {
                Debug.LogError($"Can't find grid position of Passenger {passenger}");
                return;
            }
            
            // Kiểm tra đường đi và lấy thông tin path
            if (!TryGetPathFromGridPosition(row, column, out var pathToFirstRow))
            {
                passenger.Shake(); 
                return;
            }
            
            // Tìm vị trí hàng chờ trống gần nhất
            Vector3 endFirstRowPosition = GridManager.instance.GetEndPositionOfPath(pathToFirstRow);
            (int waitingIndex, Vector3 waitingPosition) = GetNearestEmptyWaiting(endFirstRowPosition);

            if (waitingIndex == -1)
            {
                passenger.Shake();
                return;
            }

            Vehicle currentVehicle = VehicleManager.instance.GetCurrentVehicle();
            int seatIndex = VehicleManager.instance.IsVehiclesMoving() ? -1 : GetEmptySeatIndexForPassenger(passenger, currentVehicle);
            
            moveDataRef.fromRow = row;
            moveDataRef.fromColumn = column;
            moveDataRef.pathToFirstRow = pathToFirstRow;
            moveDataRef.toWaitingIndex = waitingIndex;
            moveDataRef.toWaitingPosition = waitingPosition;
            moveDataRef.preservedSeatIndex = seatIndex;
            
            _passengerMoveData.Add(passenger, moveDataRef);
            passenger.SetState(ePassengerState.MovingToFirstRow);
        }
        
        private void OnPassengerStateChanged(Passenger passenger, ePassengerState fromState, ePassengerState toState)
        {
            if (!_passengerMoveData.TryGetValue(passenger, out var moveDataRef))
            {
                Debug.LogWarning("Can't find move data of passenger");
                return;
            }
            Vehicle currentVehicle = VehicleManager.instance.GetCurrentVehicle();
            switch (fromState)
            {
                // Moving to first row
                case ePassengerState.Idle when (toState is ePassengerState.MovingToFirstRow):
                {
                    GridManager.instance.MarkCellEmpty(moveDataRef.fromRow, moveDataRef.fromColumn);
                    if (moveDataRef.preservedSeatIndex == -1) _waitingPassengers[moveDataRef.toWaitingIndex] = passenger;
                    else currentVehicle.SetReservedPassenger(moveDataRef.preservedSeatIndex, passenger);

                    Vector3[] path = new Vector3[moveDataRef.pathToFirstRow.Count];
                    for (int i = 0; i < moveDataRef.pathToFirstRow.Count; i++)
                    {
                        Vector2Int point = moveDataRef.pathToFirstRow[i];
                        Vector3 pathPosition = GridManager.instance.GetWorldPositionOfGridPosition(point.x, point.y);
                        pathPosition.y = passenger.transform.position.y;
                        path[i] = pathPosition;
                    }

                    passenger.SetRunningAnimation(true);
                    passenger.MovePath(path, Constants.PASSENGER_MOVE_SPEED).onComplete +=
                        () => passenger.SetState(ePassengerState.FirstRow);
                    break;
                }
                // Moved to first row (on the first row)
                case ePassengerState.MovingToFirstRow when (toState is ePassengerState.FirstRow):
                {
                    passenger.SetState(moveDataRef.preservedSeatIndex == -1 ? ePassengerState.MovingToQueue : ePassengerState.MovingToVehicle);
                    break;
                }
                // Moving to waiting position
                case ePassengerState.FirstRow when (toState is ePassengerState.MovingToQueue):
                {
                    Vector3 waitingPosition = moveDataRef.toWaitingPosition;
                    passenger.MoveTo(waitingPosition, Constants.PASSENGER_MOVE_SPEED).onComplete += 
                        () => passenger.SetState(ePassengerState.Waiting);
                    break;
                }
                // Moved to waiting position (on waiting queue)
                case ePassengerState.MovingToQueue when (toState is ePassengerState.Waiting):
                {
                    bool checkPassenger = CheckForPassengerMoveToVehicle(passenger, currentVehicle);
                    passenger.SetRunningAnimation(checkPassenger);
                    if (!checkPassenger)
                    {
                        Debug.Log("Level failed check 1 (click)");
                    }
                    break;
                }
                // Moving to vehicle (direct boarding)
                case ePassengerState.FirstRow when (toState is ePassengerState.MovingToVehicle):
                {
                    Vector3 doorPosition = currentVehicle.GetDoorTransform().position;
                    passenger.MoveTo(doorPosition, Constants.PASSENGER_MOVE_SPEED).onComplete +=
                        () => passenger.SetState(ePassengerState.Sitting);
                    break;
                }
                // Moving to vehicle (waiting boarding)
                case ePassengerState.Waiting when (toState is ePassengerState.MovingToVehicle):
                {
                    if (VehicleManager.instance.IsVehiclesMoving()) return;
                    
                    _waitingPassengers[moveDataRef.toWaitingIndex] = null;
                    currentVehicle.SetReservedPassenger(moveDataRef.preservedSeatIndex, passenger);
                    
                    Vector3 doorPosition = currentVehicle.GetDoorTransform().position;
                    passenger.SetRunningAnimation(true);
                    passenger.MoveTo(doorPosition, Constants.PASSENGER_MOVE_SPEED).onComplete +=
                        () => passenger.SetState(ePassengerState.Sitting);

                    break;
                }
                // Moved to vehicle (sitting on the vehicle)
                case ePassengerState.MovingToVehicle when (toState is ePassengerState.Sitting):
                {
                    Transform seatTransform = currentVehicle.GetSeatTransformAtIndex(moveDataRef.preservedSeatIndex);
                    passenger.transform.SetPositionAndRotation(seatTransform.position, Quaternion.identity);
                    passenger.transform.SetParent(seatTransform);
                    passenger.TriggerSittingAnimation();
                    currentVehicle.SetSeatOccupied(moveDataRef.preservedSeatIndex, true);
                    
                    // Check for next vehicle
                    if (VehicleManager.instance.IsVehicleFull(currentVehicle))
                    {
                        CheckForNextVehicle(currentVehicle);
                    }

                    break;
                }
                // Passenger done (destroyed)
                case ePassengerState.Sitting when (toState is ePassengerState.Inactive):
                {
                    passenger.gameObject.SetActive(false);
                    _passengerMoveData.Remove(passenger);
                    break;
                }
                // Passenger didn't spawn correctly
                case ePassengerState.None:
                {
                    Debug.LogWarning($"Passenger {passenger.name} is not spawned correctly (From {fromState} to {toState})");
                    break;
                }
                // Passenger didn't destroy correctly
                case ePassengerState.Inactive:
                {
                    Debug.LogWarning($"Passenger {passenger.name} is not destroyed correctly (From {fromState} to {toState})");
                    break;
                }
                default:
                {
                    Debug.Log($"Unhandled Passenger state change: {passenger.name} from {fromState} to {toState}");
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
                if (passenger == null || !passenger) continue;
                if (passenger.state is not ePassengerState.Waiting) continue;
                passenger.SetRunningAnimation(CheckForPassengerMoveToVehicle(passenger, vehicle));
            }
            Debug.Log("Level failed check 2 (arrival)");
        }

        private bool CheckForPassengerMoveToVehicle(Passenger passenger, Vehicle vehicle)
        {
            if (passenger == null || !passenger) return false;
            if (!_passengerMoveData.TryGetValue(passenger, out var moveDataRef)) return false;
            int seatIndex = GetEmptySeatIndexForPassenger(passenger, vehicle);
            if (seatIndex == -1) return false;
            moveDataRef.preservedSeatIndex = seatIndex;
            vehicle.SetReservedPassenger(seatIndex, passenger);
            passenger.SetState(ePassengerState.MovingToVehicle);
            return true;
        }
        
        private void OnLastVehicleLeft()
        {
            Debug.Log("Win");
        }

        private void OnCellMarkedEmpty(Cell cell, int row, int column)
        {
            Vector2Int[] directions = {
                new (1, 0),   // down
                new (0, -1),   // left
                new (-1, 0),  // up
                new (0, 1),   // right
            };
            for (int dir_index = 0; dir_index < directions.Length; dir_index++)
            {
                Vector2Int direction = directions[dir_index];
                int nextRow = row + direction.x;
                int nextColumn = column + direction.y;
                if (nextRow < 0 || nextColumn < 0 || nextRow >= _loadedLevelData.rows || nextColumn >= _loadedLevelData.columns) continue;
                
                Cell nextCell = GridManager.instance.GetCellAtGridPosition(nextRow, nextColumn);
                if (nextCell == null || !nextCell) continue;
                
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
                                tunnelPassenger.MoveTo(worldPos, Constants.PASSENGER_MOVE_SPEED);
                                
                                tunnelCell.passengers.RemoveAt(0);
                                tunnelCell.SetTunnelCount(tunnelCell.passengers.Count);
                            }
                        }
                        break;
                    }
                }
                
                Passenger nextPassenger = PassengerManager.instance.GetPassengerAtGridPosition(nextRow, nextColumn);
                if (nextPassenger == null || !nextPassenger) continue;
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

            List<Passenger> hiddenPassengers = PassengerManager.instance.GetPassengersByType(ePassengerType.Hidden);
            if (hiddenPassengers != null)
            {
                foreach (Passenger passenger in hiddenPassengers)
                {
                    if (passenger == null || !passenger) continue;
                    if (passenger.state is not ePassengerState.Idle) continue;
                    
                    (int r, int c) = PassengerManager.instance.GetGridPositionOfPassenger(passenger);
                    if (!TryGetPathFromGridPosition(r, c, out var path)) continue;
                    
                    HiddenPassenger hiddenPassenger = (HiddenPassenger) passenger;
                    if (path.Count == 0)
                    {
                        hiddenPassenger.SetRevealedImmediately();
                        continue;
                    }
                    
                    // if has path then reveal passenger
                    if (!hiddenPassenger.IsRevealed())
                    {
                        Color toColor = GameManager.GetColorByType(hiddenPassenger.data.colorType);
                        hiddenPassenger.Reveal(toColor);
                    }
                }
            }
            
            List<Passenger> cloakPassengers = PassengerManager.instance.GetPassengersByType(ePassengerType.Cloak);
            if (cloakPassengers != null)
            {
                foreach (Passenger passenger in cloakPassengers)
                {
                    if (passenger == null || !passenger) continue;
                    if (passenger.state is not ePassengerState.Idle) continue;
    
                    CloakPassenger cloakPassenger = (CloakPassenger) passenger;
                    CloakPassengerData cloakData = (CloakPassengerData) cloakPassenger.data.extraData;
                    cloakData.isRevealed = !cloakData.isRevealed;
                    _ = cloakData.isRevealed ? cloakPassenger.CloakOff() : cloakPassenger.CloakOn();
                }
            }
            
            List<Passenger> bombPassengers = PassengerManager.instance.GetPassengersByType(ePassengerType.Bomb);
            if (bombPassengers != null)
            {
                foreach (Passenger passenger in bombPassengers)
                {
                    if (passenger == null || !passenger) continue;
                    if (passenger.state is not ePassengerState.Idle) continue;
                    
                    BombPassenger bombPassenger = (BombPassenger) passenger;
                    BombPassengerData bombData = (BombPassengerData) bombPassenger.data.extraData;
                    bombData.bombTime = Mathf.Max(0, bombData.bombTime - 1);
                    bombPassenger.SetBombTime(bombData.bombTime);
                    
                    Debug.Log("Level failed check 3 (bomb)");
                }
            }
        }

        private void OnCellMarkedOccupied(Cell cell, int row, int column)
        {
            
        }

        private void OnNewWaitingTileAdded(int newCount)
        {
            if (_waitingPassengers.Count >= newCount) return;
            
            // Add new passengers to waiting queue
            _waitingPassengers.Add(null);
            
            // Change waiting passenger's position to match the new count
            for (int i = 0; i < newCount; i++)
            {
                Passenger passenger = _waitingPassengers[i];
                if (passenger == null || !passenger) continue;
                Vector3? waitingPosition = GridManager.instance.GetPositionOfWaitingTileIndex(i);
                if (!waitingPosition.HasValue) continue;
                passenger.transform.SetPositionAndRotation(waitingPosition.Value, Quaternion.identity);
            }
        }

        private void CheckForNextVehicle(Vehicle currentVehicle)
        {
            VehicleManager.instance.NextVehicleSequence().onComplete += () =>
            {
                for (int i = 0; i < currentVehicle.reservedPassengers.Length; i++)
                {
                    Passenger passenger = currentVehicle.reservedPassengers[i];
                    if (passenger == null || !passenger) continue;
                    passenger.SetState(ePassengerState.Inactive);
                }
            };
        }
        
        private bool TryGetPathFromGridPosition(int r, int c, out IReadOnlyList<Vector2Int> path)
        {
            path = GridManager.instance.GetPathToFirstRow(r, c);
            return (path != null);
        }
        
        private (int, Vector3) GetNearestEmptyWaiting(Vector3 fromPosition)
        {
            float smallestDistance = float.MaxValue;
            int nearestIndex = -1;
            Vector3 nearestPosition = Vector3.zero;
            
            for (int i = 0; i < _waitingPassengers.Count; i++)
            {
                if (_waitingPassengers[i] != null || _waitingPassengers[i]) continue;
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
            if (VehicleManager.instance.IsVehicleFull(vehicle) || !passenger || !vehicle) return -1;

            for (int i = 0; i < Constants.VEHICLE_SEAT_SLOTS; i++)
            {
                if (vehicle.seatOccupied[i] || vehicle.reservedPassengers[i]) continue;
                if (vehicle.data.colorType == passenger.data.colorType) return i;
            }
            return -1;
        }
    }
}