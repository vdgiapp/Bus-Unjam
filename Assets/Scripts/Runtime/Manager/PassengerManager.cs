using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class PassengerManager : MonoBehaviour
    {
        [SerializeField] private Transform _passengerContainer;
        [SerializeField] private Transform _cellContainer;

        private Passenger[,] _passengerGrid;
        private readonly Dictionary<Passenger, Vector2Int> _passengerPositionMap = new();

        private int _rows;
        private int _columns;

        public async UniTask LoadPassengerFromLevelAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            
            // Initialize grid
            _rows = levelData.rows;
            _columns = levelData.columns;
            _passengerGrid = new Passenger[_rows, _columns];
            
            // Spawn passengers
            List<UniTask> tasks = new();
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    if (ShouldSpawnPassengerAt(levelData, row, col))
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
            
            // Configure passenger
            passenger.data = data;
            passenger.SetColor(GameManager.GetColorByType(data.colorType));
            
            // Register passenger
            _passengerGrid[row, col] = passenger;
            _passengerPositionMap.Add(passenger, new Vector2Int(row, col));
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
        
        private bool ShouldSpawnPassengerAt(LevelData levelData, int row, int col)
        {
            CellData cellData = levelData.GetCellData(row, col);
            
            // TODO: Variant type handle
            return !Utilities.IsCellTypeIgnoreOccupied(cellData.cellType);
        }

        public Passenger GetPassengerAtGridPosition(int row, int column)
        {
            if (!IsValidGridPosition(row, column)) return null;
            return _passengerGrid[row, column];
        }

        public Vector2Int? GetGridPositionOfPassenger(Passenger passenger)
        {
            if (_passengerPositionMap.TryGetValue(passenger, out Vector2Int position))
            {
                return position;
            }
            return null;
        }
    }
}