using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BusUnjam
{
    [DisallowMultipleComponent]
    public class PassengerManager : MonoBehaviour
    {
        private const float PASSENGER_DISTANCE = 0.6f;
        
        [SerializeField] private Transform _passengerContainer;
        [SerializeField] private Transform _cellContainer;

        private Passenger[,] _passengers;
        private readonly Dictionary<Passenger, Vector2Int> _passengerGridPositions = new();

        private int _rows;
        private int _columns;

        public async UniTask LoadPassengerFromLevelAsync(LevelData levelData)
        {
            if (levelData == null || levelData.passengers == null || levelData.passengers.Length == 0) return;
            _rows = levelData.rows;
            _columns = levelData.columns;
            _passengers = new Passenger[_rows, _columns];
            
            List<UniTask> tasks = new();
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _columns; c++)
                {
                    CellData cellData = levelData.GetCellData(r, c);
                    PassengerData data = levelData.GetPassengerData(r, c);
                    // TODO: Variant type handle
                    if (!Utilities.IsCellTypeIgnoreOccupied(cellData.cellType))
                        tasks.Add(LoadSinglePassengerAsync(r, c, data));
                }
            }
            await UniTask.WhenAll(tasks);
        }

        private async UniTask LoadSinglePassengerAsync(int row, int col, PassengerData data)
        {
            Vector3 worldPosition = Utilities.GridToWorldXZNeg(_columns, row, col, PASSENGER_DISTANCE, _cellContainer.position);
            GameObject prefab = GameManager.GetCurrentTheme().GetPassengerPrefabByType(data.passengerType);
            GameObject[] loaded = 
                await InstantiateAsync(prefab, _passengerContainer, worldPosition, Quaternion.identity)
                    .ToUniTask();
            Passenger p = loaded[0].GetComponent<Passenger>();
            p.data = data;
            p.SetColor(GameManager.GetColorByType(data.colorType));
            _passengers[row, col] = p;
            _passengerGridPositions.Add(p, new Vector2Int(row, col));
        }

        public Passenger GetPassengerAtGridPosition(int row, int col)
            => Utilities.IsInBounds(_rows, _columns, row, col) ? _passengers[row, col] : null;

        public Vector2Int? GetGridPositionOfPassenger(Passenger p)
            => _passengerGridPositions.TryGetValue(p, out Vector2Int pos) ? pos : null;
    }
}