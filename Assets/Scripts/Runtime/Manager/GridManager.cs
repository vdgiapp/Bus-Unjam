using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class GridManager : MonoBehaviour
    {
        public static GridManager instance { get; private set; }
        
        public static event Action<int> NewWaitingTileAdded;
        public static event Action<Cell, int, int> CellMarkedEmpty;
        public static event Action<Cell, int, int> CellMarkedOccupied;
        
        [SerializeField] private Transform _waitingContainer;
        [SerializeField] private Transform _cellContainer;

        private Cell[,] _cellGrid;
        private readonly Dictionary<Cell, (int, int)> _cellPositionMap = new();
        private readonly List<Transform> _waitingTiles = new();
        private LevelData _loadedLevelData;
        
        private bool _isInstanceInit = false;
        private bool _isWaitForInstanceInit = false;

        private void Awake()
        {
            instance = this;
            _isInstanceInit = true;
        }

        public async UniTask LoadCellFromLevelAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            if (!await WaitForInstanceInit()) return;
            
            // Initialize grid
            _loadedLevelData = levelData;
            _cellGrid = new Cell[levelData.rows, levelData.columns];
            _cellPositionMap.Clear();
            _waitingTiles.Clear();
            
            // Spawn cells
            List<UniTask> tasks = new();
            for (int row = 0; row < levelData.rows; row++)
            {
                for (int col = 0; col < levelData.columns; col++)
                {
                    CellData cellData = levelData.GetCellData(row, col);
                    if (cellData.cellType != eCellType.None)
                    {
                        tasks.Add(SpawnCellAsync(row, col, cellData));
                    }
                }
            }
            await UniTask.WhenAll(tasks);
        }

        private async UniTask SpawnCellAsync(int row, int col, CellData data)
        {
            Vector3 worldPosition = CalculateWorldPosition(row, col);
            GameObject prefab = GetCellPrefab(data.cellType);

            Cell cell = await InstantiateCellAsync(prefab, worldPosition);
            cell.name = $"{Constants.CELL_GAMEOBJECT_NAME} ({row}, {col})";
            cell.InitData(data);

            switch (cell)
            {
                case TunnelCell tunnelCell
                    when (data is { cellType: eCellType.Tunnel, extraData: TunnelCellData tunnelData }):
                {
                    tunnelCell.SetTunnelDirection(tunnelData.direction);
                    tunnelCell.SetTunnelCount(tunnelData.passengers.Count);

                    for (int i = 0; i < tunnelData.passengers.Count; i++)
                    {
                        PassengerData passengerData = tunnelData.passengers[i];
                        Passenger passenger = await SpawnTunnelPassengerAsync(i, row, col, passengerData, null);
                        tunnelCell.passengers.Add(passenger);
                    }
                    break;
                }
            }
            _cellGrid[row, col] = cell;
            _cellPositionMap.Add(cell, (row, col));
        }
        
        private async UniTask<Passenger> SpawnTunnelPassengerAsync(int index, int row, int col, PassengerData data, Transform parent)
        {
            Vector3 worldPosition = CalculateWorldPosition(row, col);
            Passenger passenger = await InstantiateTunnelPassengerAsync(GetNormalPassengerPrefab(), parent, worldPosition);
            passenger.name = $"(Tunnel at {row}, {col}) - {Constants.PASSENGER_GAMEOBJECT_NAME} ({index})";
            passenger.InitData(data);
            passenger.SetStateWithoutNotify(ePassengerState.Idle);
            passenger.SetColor(GameManager.GetColorByType(data.colorType));
            passenger.transform.localScale = Vector3.one * 0.5f;
            passenger.gameObject.SetActive(false);
            return passenger;
        }
        
        public async UniTask LoadWaitingTileAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            await WaitForInstanceInit();
            
            // Compute waiting area size
            int size = levelData.waitAreaSize;
            float half = (size - 1) / 2f;
            
            // Spawn waiting tiles
            List<UniTask> tasks = new();
            for (int i = 0; i < size; i++)
            {
                tasks.Add(SpawnWaitingTileAsync(i, half));
            }
            await UniTask.WhenAll(tasks);
        }

        private async UniTask SpawnWaitingTileAsync(int index, float half)
        {
            // Compute world position for each waiting slot
            Vector3 pos = _waitingContainer.position + new Vector3((index - half) * Constants.CELL_DISTANCE, 0f, 0f);
            GameObject prefab = GetWaitingTilePrefab();
            GameObject[] loaded = await InstantiateAsync(prefab, _waitingContainer, pos, Quaternion.identity).ToUniTask();
            loaded[0].name = $"{Constants.WAITING_TILE_GAMEOBJECT_NAME} ({index})";
            _waitingTiles.Add(loaded[0].transform);
        }

        public async UniTask AddNewWaitingTile()
        {
            // Calculate new size and position of waiting tiles
            int size = _waitingTiles.Count + 1;
            float half = (size - 1) / 2f;
            await SpawnWaitingTileAsync(_waitingTiles.Count, half);
            
            // Reposition waiting tiles
            for (int i = 0; i < _waitingTiles.Count; i++)
            {
                Transform t = _waitingTiles[i];
                Vector3 pos = _waitingContainer.position + new Vector3((i - half) * Constants.CELL_DISTANCE, 0f, 0f);
                t.position = pos;
            }
            NewWaitingTileAdded?.Invoke(_waitingTiles.Count);
        }

        /// <summary>
        /// Finds a path from the given start cell to the top row of the cell using BFS. 
        /// Returns a list of cell coordinates to move through, or null if no path exists.
        /// </summary>
        public IReadOnlyList<Vector2Int> GetPathToFirstRow(int startRow, int startColumn)
        {
            if (!IsValidGridPosition(startRow, startColumn)) return null;
            
            Vector2Int startPos = new Vector2Int(startRow, startColumn);
            Vector2Int? endPos = null;
            Dictionary<Vector2Int, Vector2Int> travelDictionary = new();
            Queue<Vector2Int> bfsQueue = new();
            
            Vector2Int[] directions = {
                new (1, 0),   // move down
                new (-1, 0),  // move up
                new (0, 1),   // move right
                new (0, -1)   // move left
            };
            
            travelDictionary[startPos] = startPos;
            bfsQueue.Enqueue(startPos);
            
            // Breadth-first search
            while (bfsQueue.Count > 0)
            {
                Vector2Int current = bfsQueue.Dequeue();
                
                // Check if reached end
                if (current.x == 0)
                {
                    endPos = current;
                    break;
                }
                
                // Check neighbors
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int next = current + dir;
                    if (!IsValidGridPosition( next.x, next.y)) continue;
                    if (travelDictionary.ContainsKey(next)) continue;
                    
                    Cell c = _cellGrid[next.x, next.y];
                    if (!c) continue;
                    if (c.data.isOccupied) continue;
                    if (Utilities.IsCellTypeIgnoreOccupied(c.data.cellType)) continue;
                    
                    travelDictionary[next] = current;
                    bfsQueue.Enqueue(next);
                }
            }
            
            // No path found
            if (!endPos.HasValue) return null;
            
            // Build path
            List<Vector2Int> path = new();
            for (Vector2Int v = endPos.Value; v != startPos; v = travelDictionary[v]) path.Add(v);
            path.Reverse();
            
            return path;
        }
        
        private async UniTask<Cell> InstantiateCellAsync(GameObject prefab, Vector3 position)
        {
            GameObject[] loaded = await InstantiateAsync(prefab, _cellContainer, position, Quaternion.identity).ToUniTask();
            return loaded[0].GetComponent<Cell>();
        }
        
        private async UniTask<Passenger> InstantiateTunnelPassengerAsync(GameObject prefab, Transform parent, Vector3 position)
        {
            GameObject[] loaded = await InstantiateAsync(prefab, parent, position, Quaternion.identity).ToUniTask();
            return loaded[0].GetComponent<Passenger>();
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

        private async UniTask<bool> WaitForInstanceInit()
        {
            if (_isWaitForInstanceInit) return false;
            if (_isInstanceInit) return true;
            _isWaitForInstanceInit = true;
            await UniTask.WaitUntil(() => _isInstanceInit);
            _isWaitForInstanceInit = false;
            return true;
        }
        
        private GameObject GetCellPrefab(eCellType cellType)
        {
            return GameManager.GetCurrentTheme().GetCellPrefabByType(cellType);
        }
        
        private GameObject GetNormalPassengerPrefab()
        {
            return GameManager.GetCurrentTheme().GetPassengerPrefabByType(ePassengerType.Normal);
        }
        
        private GameObject GetWaitingTilePrefab()
        {
            return GameManager.GetCurrentTheme().waitingTilePrefab;
        }

        private bool IsValidLevelData(LevelData levelData)
        {
            return levelData.cells is { Count: > 0 };
        }
        
        private bool IsValidGridPosition(int row, int col)
        {
            return Utilities.IsInBounds(_loadedLevelData.rows, _loadedLevelData.columns, row, col);
        }

        public void MarkCellEmpty(int row, int column)
        {
            if (!IsValidGridPosition(row, column)) return;
            _cellGrid[row, column].data.isOccupied = false;
            CellMarkedEmpty?.Invoke(_cellGrid[row, column], row, column);
        }

        public void MarkCellOccupied(int row, int column)
        {
            if (!IsValidGridPosition(row, column)) return;
            _cellGrid[row, column].data.isOccupied = true;
            CellMarkedOccupied?.Invoke(_cellGrid[row, column], row, column);
        }

        public Cell GetCellAtGridPosition(int row, int column)
        {
            return IsValidGridPosition(row, column) ? _cellGrid[row, column] : null;
        }

        public (int row, int column) GetGridPositionOfCell(Cell c)
        {
            return _cellPositionMap.GetValueOrDefault(c, (-1, -1));
        }

        public Vector3? GetPositionOfWaitingTileIndex(int index)
        {
            if (index < 0 || index >= _waitingTiles.Count) return null;
            return _waitingTiles[index].position;
        }
        
        public Vector3 GetWorldPositionOfGridPosition(int row, int col)
        {
            return Utilities.GridToWorldXZNeg(
                _loadedLevelData.columns, row, col, 
                Constants.CELL_DISTANCE, 
                _cellContainer.position
            );
        }
        
        public Vector3 GetEndPositionOfPath(IReadOnlyList<Vector2Int> path)
        {
            if (path.Count == 0) return GetWorldPositionOfGridPosition(0, 0);
            Vector2Int lastPoint = path[^1];
            return GetWorldPositionOfGridPosition(lastPoint.x, lastPoint.y);
        }
    }
}