using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BusUnjam
{
    [DisallowMultipleComponent]
    public class GridManager : MonoBehaviour
    {
        public event Action OnAllLevelCellLoaded;
        public event Action OnAllLevelWaitingTileLoaded;

        [SerializeField] private Transform _waitingContainer;
        [SerializeField] private Transform _cellContainer;
        
        private Cell[,] _cells;
        private readonly Dictionary<Cell, Vector2Int> _cellGridPositions = new();
        
        private readonly List<Vector3> _waitingTilePositions = new();

        private int _rows;
        private int _columns;

        public async UniTask LoadCellFromLevelAsync(LevelData levelData)
        {
            if (levelData == null || levelData.cells == null || levelData.cells.Length == 0) return;
            _rows = levelData.rows;
            _columns = levelData.columns;

            _cells = new Cell[_rows, _columns];
            
            List<UniTask> tasks = new();
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _columns; c++)
                {
                    CellData data = levelData.GetCellData(r, c);
                    if (data.cellType != eCellType.None)
                        tasks.Add(LoadSingleCellAsync(r, c, data));
                }
            }
            await UniTask.WhenAll(tasks);
            
            OnAllLevelCellLoaded?.Invoke();
        }

        private async UniTask LoadSingleCellAsync(int row, int col, CellData data)
        {
            Vector3 worldPosition = Utilities.GridToWorldXZNeg(_columns, row, col, Constants.CellDistance, _cellContainer.position);
            GameObject prefab = GameManager.GetCurrentTheme().GetCellPrefabByType(data.cellType);
            GameObject[] loaded =
                await InstantiateAsync(prefab, _cellContainer, worldPosition, Quaternion.identity)
                    .ToUniTask();
            Cell c = loaded[0].GetComponent<Cell>();
            c.data = data;
            _cells[row, col] = c;
            _cellGridPositions.Add(c, new Vector2Int(row, col));
        }
        
        public async UniTask LoadWaitingTileAsync(LevelData levelData)
        {
            int size = levelData.waitAreaSize;
            float half = (size - 1) / 2f;
            UniTask[] tasks = new UniTask[size];
            for (int i = 0; i < size; i++) tasks[i] = LoadSingleWaitingTileAsync(i, half);
            await UniTask.WhenAll(tasks);
            OnAllLevelWaitingTileLoaded?.Invoke();
        }

        private async UniTask LoadSingleWaitingTileAsync(int index, float half)
        {
            // Compute world position for each waiting slot
            Vector3 pos = _waitingContainer.position + new Vector3((index - half) * Constants.CellDistance, 0f, 0f);
            GameObject prefab = GameManager.GetCurrentTheme().GetWaitingTilePrefab();
            GameObject[] loaded =
                await InstantiateAsync(prefab, _waitingContainer, pos, Quaternion.identity)
                    .ToUniTask();
            _waitingTilePositions.Add(pos);
        }

        /// <summary>
        /// Finds a path from the given start cell to the top row of the cell using BFS. 
        /// Returns a list of cell coordinates to move through, or null if no path exists.
        /// </summary>
        public IReadOnlyList<Vector2Int> FindPathToFirstRow(int startRow, int startColumn)
        {
            if (!Utilities.IsInBounds(_rows, _columns, startRow, startColumn)) return null;
            Vector2Int[] directions = 
            {
                new Vector2Int(1, 0),   // move down
                new Vector2Int(-1, 0),  // move up
                new Vector2Int(0, 1),   // move right
                new Vector2Int(0, -1)   // move left
            };
            Dictionary<Vector2Int, Vector2Int> travelDictionary = new();
            Queue<Vector2Int> bfsQueue = new();
            
            Vector2Int startPos = new Vector2Int(startRow, startColumn);
            Vector2Int? endPos = null;
            
            bfsQueue.Enqueue(startPos);
            travelDictionary[startPos] = startPos;
            
            // Breadth-first search
            while (bfsQueue.Count > 0)
            {
                Vector2Int current = bfsQueue.Dequeue();
                if (current.x == 0)
                {
                    endPos = current;
                    break; // reached top row
                }
                // check valid direction
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int next = current + dir;
                    if (!Utilities.IsInBounds(_rows, _columns, next.x, next.y)) continue;
                    if (travelDictionary.ContainsKey(next)) continue;
                    
                    Cell c = _cells[next.x, next.y];
                    
                    if (c == null || c.data == null) continue;
                    if (c.data.isOccupied) continue;
                    if (Utilities.IsCellTypeOccupied(c.data.cellType)) continue;
                    
                    travelDictionary[next] = current;
                    bfsQueue.Enqueue(next);
                }
            }
            if (!endPos.HasValue) return null;
            List<Vector2Int> path = new();
            for (Vector2Int v = endPos.Value; v != startPos; v = travelDictionary[v]) path.Add(v);
            path.Reverse();
            return path;
        }

        public void MarkCellEmpty(int row, int col)
        {
            if (Utilities.IsInBounds(_rows, _columns, row, col))
                _cells[row, col].data.isOccupied = false; 
        }

        public Cell GetCellAtGridPosition(int row, int col)
            => Utilities.IsInBounds(_rows, _columns, row, col) ? _cells[row, col] : null;

        public Vector2Int? GetGridPositionOfCell(Cell c)
            => _cellGridPositions.TryGetValue(c, out Vector2Int pos) ? pos : null;

        public int GetWaitingTileIndexAtPosition(Vector3 pos)
        {
            for (int i = 0; i < _waitingTilePositions.Count; i++)
            {
                if (_waitingTilePositions[i] == pos)
                    return i;
            }
            return -1;
        }

        public Vector3? GetPositionOfWaitingTileIndex(int index)
        {
            if (index < 0 && index >= _waitingTilePositions.Count) return null;
            return _waitingTilePositions[index];
        }
    }
}