using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class PassengerManager : MonoBehaviour
    {
        public enum ePassengerState
        {
            None = -1,
            Idle,
            Moving,
            Waiting,
            Boarding,
            Sitting,
            Inactive,
        }
        
        [SerializeField] private Transform _passengerContainer;
        [SerializeField] private Transform _cellContainer;

        private Passenger[,] _passengerGrid;
        private readonly Dictionary<Passenger, (int, int)> _passengerPositionMap = new();
        private readonly Dictionary<Passenger, ePassengerState> _passengerStates = new();

        private int _rows;
        private int _columns;

        public async UniTask LoadPassengerFromLevelAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            
            // Initialize grid
            _rows = levelData.rows;
            _columns = levelData.columns;
            _passengerGrid = new Passenger[_rows, _columns];
            _passengerPositionMap.Clear();
            _passengerStates.Clear();
            
            // Spawn passengers
            List<UniTask> tasks = new();
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
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
            passenger.InitData(data);
            passenger.SetColor(GameManager.GetColorByType(data.colorType));

            switch (passenger)
            {
                case RopePassenger ropePassenger
                    when (data is { passengerType: ePassengerType.Rope, extraData: RopePassengerData ropeData }):
                {
                    ropePassenger.SetRopeCount(ropeData.ropeCount);
                    break;
                }
                case HiddenPassenger hiddenPassenger
                    when (data is { passengerType: ePassengerType.Hidden }):
                {
                    hiddenPassenger.SetConcealedImmediately();
                    break;
                }
                case BombPassenger bombPassenger
                    when (data is { passengerType: ePassengerType.Bomb, extraData: BombPassengerData bombData }):
                {
                    bombPassenger.SetBombTime(bombData.bombTime);
                    break;
                }
                case CloakPassenger cloakPassenger
                    when (data is { passengerType: ePassengerType.Cloak, extraData: CloakPassengerData cloakData }):
                {
                    cloakPassenger.SetCloakImmediately(cloakData.isRevealed);
                    break;
                }
            }
            
            _passengerGrid[row, col] = passenger;
            _passengerPositionMap.Add(passenger, (row, col));
            _passengerStates.Add(passenger, ePassengerState.Idle);
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
        
        private async UniTask<Passenger> InstantiatePassengerAsync(GameObject prefab, Vector3 position)
        {
            GameObject[] loaded = await InstantiateAsync(prefab, _passengerContainer, position, Quaternion.identity).ToUniTask();
            return loaded[0].GetComponent<Passenger>();
        }
        
        private GameObject GetPassengerPrefab(ePassengerType passengerType)
        {
            return GameManager.GetCurrentTheme()?.GetPassengerPrefabByType(passengerType);
        }
        
        private bool IsValidGridPosition(int row, int col)
        {
            return Utilities.IsInBounds(_rows, _columns, row, col);
        }
        
        private bool IsValidLevelData(LevelData levelData)
        {
            return levelData.passengers is { Count: > 0 };
        }

        public Passenger GetPassengerAtGridPosition(int row, int column)
        {
            if (!IsValidGridPosition(row, column)) return null;
            return _passengerGrid[row, column];
        }
        
        public ePassengerState GetStateOfPassenger(Passenger passenger)
        {
            return _passengerStates.GetValueOrDefault(passenger, ePassengerState.None);
        }
        
        public void SetStateOfPassenger(Passenger passenger, ePassengerState state)
        {
            if (!_passengerStates.ContainsKey(passenger)) return;
            _passengerStates[passenger] = state;
        }

        public (int row, int column) GetGridPositionOfPassenger(Passenger passenger)
        {
            return _passengerPositionMap.GetValueOrDefault(passenger, (-1, -1));
        }
    }
}