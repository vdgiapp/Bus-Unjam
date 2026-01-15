using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class PassengerManager : MonoBehaviour
    {
        public static PassengerManager instance { get; private set; }
        
        [SerializeField] private Transform _passengerContainer;
        [SerializeField] private Transform _cellContainer;

        private Passenger[,] _passengerGrid;
        private readonly Dictionary<Passenger, (int, int)> _passengerPositionMap = new();
        private LevelData _loadedLevelData;
        private readonly Dictionary<ePassengerType, List<Passenger>> _passengerByType = new();
        
        private bool _isInstanceInit = false;
        private bool _isWaitForInstanceInit = false;
        
        private void Awake()
        {
            instance = this;
            _isInstanceInit = true;
        }

        public async UniTask LoadPassengerFromLevelAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            if (!await WaitForInstanceInit()) return;
            
            // Initialize grid
            _loadedLevelData = levelData;
            _passengerGrid = new Passenger[levelData.rows, levelData.columns];
            _passengerPositionMap.Clear();
            
            // Spawn passengers
            List<UniTask> tasks = new();
            for (int row = 0; row < levelData.rows; row++)
            {
                for (int col = 0; col < levelData.columns; col++)
                {
                    CellData cellData = levelData.GetCellData(row, col);
                    if (!Utilities.IsCellTypeIgnoreOccupied(cellData.cellType))
                    {
                        PassengerData passengerData = levelData.GetPassengerData(row, col);
                        tasks.Add(SpawnPassengerAsync(row, col, passengerData));
                    }
                }
            }
            await UniTask.WhenAll(tasks);
        }
        
        private async UniTask SpawnPassengerAsync(int row, int col, PassengerData data)
        {
            Vector3 worldPosition = CalculateWorldPosition(row, col);
            GameObject prefab = GetPassengerPrefab(data.passengerType);
            
            Passenger passenger = await InstantiatePassengerAsync(prefab, worldPosition);
            passenger.name = $"{Constants.PASSENGER_GAMEOBJECT_NAME} ({row}, {col}) - {data.passengerType}";
            passenger.InitData(data);
            passenger.SetStateWithoutNotify(ePassengerState.Idle);
            passenger.SetColor(GameManager.GetColorByType(data.colorType));

            switch (passenger)
            {
                case RopePassenger ropePassenger
                    when (data is { passengerType: ePassengerType.Rope, extraData: RopePassengerData ropeData }):
                {
                    ropePassenger.SetRopeCount(ropeData.ropeCount);
                    AddPassengerByType(ePassengerType.Rope, ropePassenger);
                    break;
                }
                case HiddenPassenger hiddenPassenger
                    when (data is { passengerType: ePassengerType.Hidden }):
                {
                    hiddenPassenger.SetConcealedImmediately();
                    AddPassengerByType(ePassengerType.Hidden, hiddenPassenger);
                    break;
                }
                case BombPassenger bombPassenger
                    when (data is { passengerType: ePassengerType.Bomb, extraData: BombPassengerData bombData }):
                {
                    bombPassenger.SetBombTime(bombData.bombTime);
                    AddPassengerByType(ePassengerType.Bomb, bombPassenger);
                    break;
                }
                case CloakPassenger cloakPassenger
                    when (data is { passengerType: ePassengerType.Cloak, extraData: CloakPassengerData cloakData }):
                {
                    cloakPassenger.SetCloakImmediately(!cloakData.isRevealed);
                    AddPassengerByType(ePassengerType.Cloak, cloakPassenger);
                    break;
                }
            }
            
            _passengerGrid[row, col] = passenger;
            _passengerPositionMap.Add(passenger, (row, col));
        }

        private Vector3 CalculateWorldPosition(int row, int col)
        {
            return Utilities.GridToWorldXZNeg(
                _loadedLevelData.columns, 
                row, 
                col, 
                Constants.CELL_DISTANCE, 
                _cellContainer.position
            );
        }
        
        private async UniTask<Passenger> InstantiatePassengerAsync(GameObject prefab, Vector3 position)
        {
            GameObject[] loaded = await InstantiateAsync(prefab, _passengerContainer, position, Quaternion.identity).ToUniTask();
            return loaded[0].GetComponent<Passenger>();
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

        private void AddPassengerByType(ePassengerType type, Passenger passenger)
        {
            if (!_passengerByType.ContainsKey(type))
            {
                _passengerByType.Add(type, new List<Passenger>());
            }
            _passengerByType[type].Add(passenger);
        }
        
        private GameObject GetPassengerPrefab(ePassengerType passengerType)
        {
            return GameManager.GetCurrentTheme().GetPassengerPrefabByType(passengerType);
        }
        
        private bool IsValidGridPosition(int row, int col)
        {
            return Utilities.IsInBounds(_loadedLevelData.rows, _loadedLevelData.columns, row, col);
        }
        
        private bool IsValidLevelData(LevelData levelData)
        {
            return levelData.passengers is { Count: > 0 };
        }
        
        public bool IsPassengerBusy(Passenger passenger)
        {
            ePassengerState state = passenger.state;
            return (state is not (ePassengerState.None or ePassengerState.Idle or ePassengerState.Inactive));
        }
        
        public Passenger GetPassengerAtGridPosition(int row, int column)
        {
            if (!IsValidGridPosition(row, column)) return null;
            return _passengerGrid[row, column];
        }

        public (int row, int column) GetGridPositionOfPassenger(Passenger passenger)
        {
            return _passengerPositionMap.GetValueOrDefault(passenger, (-1, -1));
        }
        
        public List<Passenger> GetPassengersByType(ePassengerType type)
        {
            return _passengerByType.GetValueOrDefault(type, null);
        }
        
        public void SetPassengerAtGridPosition(int row, int column, Passenger passenger)
        {
            if (!IsValidGridPosition(row, column)) return;
            _passengerGrid[row, column] = passenger;
            _passengerPositionMap[passenger] = (row, column);
        }
    }
}