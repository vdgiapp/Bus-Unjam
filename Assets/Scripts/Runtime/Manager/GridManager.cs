using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private Transform _waitingContainer;
        [SerializeField] private Transform _cellContainer;
        
        private Cell[,] _cellGrid;
        private readonly Dictionary<Cell, Vector2Int> _cellPositionMap = new();
        
        private readonly List<Vector3> _waitingTilePositions = new();

        private int _rows;
        private int _columns;

        public async UniTask LoadCellFromLevelAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            
            // Initialize grid
            _rows = levelData.rows;
            _columns = levelData.columns;
            _cellGrid = new Cell[_rows, _columns];
            
            // Spawn cells
            List<UniTask> tasks = new();
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    if (ShouldSpawnCellAt(levelData, row, col))
                    {
                        CellData data = levelData.GetCellData(row, col);
                        tasks.Add(SpawnCellAsync(row, col, data));
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
            
            // Configure cell
            cell.data = data;
            
            // Register cell
            _cellGrid[row, col] = cell;
            _cellPositionMap.Add(cell, new Vector2Int(row, col));
        }
        
        public async UniTask LoadWaitingTileAsync(LevelData levelData)
        {
            if (!IsValidLevelData(levelData)) return;
            
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
            await InstantiateAsync(prefab, _waitingContainer, pos, Quaternion.identity).ToUniTask();
            _waitingTilePositions.Add(pos);
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
                    if (c == null) continue;
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
        
        private GameObject GetCellPrefab(eCellType cellType)
        {
            return GameManager.GetCurrentTheme()?.GetCellPrefabByType(cellType);
        }
        
        private GameObject GetWaitingTilePrefab()
        {
            return GameManager.GetCurrentTheme()?.waitingTilePrefab();
        }

        private bool IsValidLevelData(LevelData levelData)
        {
            return levelData.cells is { Count: > 0 };
        }
        
        private bool IsValidGridPosition(int row, int col)
        {
            return Utilities.IsInBounds(_rows, _columns, row, col);
        }
        
        private bool ShouldSpawnCellAt(LevelData levelData, int row, int col)
        {
            CellData cellData = levelData.GetCellData(row, col);
            
            // TODO: Variant type handle
            return cellData.cellType != eCellType.None;
        }

        public void MarkCellEmpty(int row, int column)
        {
            if (IsValidGridPosition( row, column))
            {
                _cellGrid[row, column].data.isOccupied = false;
            }
        }

        public void MarkCellOccupied(int row, int column)
        {
            if (IsValidGridPosition( row, column))
            {
                _cellGrid[row, column].data.isOccupied = true;
            }
        }

        public Cell GetCellAtGridPosition(int row, int column)
        {
            if (IsValidGridPosition( row, column)) return _cellGrid[row, column];
            return null;
        }

        public Vector2Int? GetGridPositionOfCell(Cell c)
        {
            if (_cellPositionMap.TryGetValue(c, out Vector2Int pos)) return pos;
            return null;
        }

        public int GetWaitingTileIndexAtPosition(Vector3 position)
        {
            for (int i = 0; i < _waitingTilePositions.Count; i++) if (_waitingTilePositions[i] == position) return i;
            return -1;
        }

        public Vector3? GetPositionOfWaitingTileIndex(int index)
        {
            if (index < 0 || index >= _waitingTilePositions.Count) return null;
            return _waitingTilePositions[index];
        }
    }
}