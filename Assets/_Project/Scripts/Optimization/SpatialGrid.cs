using System.Collections.Generic;
using UnityEngine;

namespace BehaviorSimulation.Optimization
{
    // Uniform spatial hash for O(1) amortized neighbor insertion and O(k) query,
    // where k = average agents in the 3×3 cell neighborhood around the query point.
    // Reusing List<int> objects from a pool avoids per-frame GC allocations.
    public sealed class SpatialGrid
    {
        readonly Dictionary<long, List<int>> _cells = new(256);
        readonly List<List<int>>             _pool  = new(64);

        float _invCell;
        int   _activeCells;

        public int ActiveCells => _activeCells;

        // Call once per tick before inserting agents.
        public void Reset(float cellSize)
        {
            _invCell = 1f / cellSize;
            foreach (var list in _cells.Values)
            {
                list.Clear();
                _pool.Add(list);
            }
            _cells.Clear();
            _activeCells = 0;
        }

        public void Insert(int agentIndex, float x, float y)
        {
            long key = Hash(x, y);
            if (!_cells.TryGetValue(key, out var list))
            {
                list = _pool.Count > 0 ? PopPool() : new List<int>(8);
                _cells[key] = list;
                _activeCells++;
            }
            list.Add(agentIndex);
        }

        // Fills results with indices of all agents in the 3×3 cell neighborhood.
        public void Query(float x, float y, List<int> results)
        {
            results.Clear();
            int cx = CellCoord(x), cy = CellCoord(y);
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                long key = MakeKey(cx + dx, cy + dy);
                if (!_cells.TryGetValue(key, out var list)) continue;
                for (int k = 0; k < list.Count; k++)
                    results.Add(list[k]);
            }
        }

        int  CellCoord(float v)          => Mathf.FloorToInt(v * _invCell);
        long Hash(float x, float y)       => MakeKey(CellCoord(x), CellCoord(y));
        static long MakeKey(int cx, int cy) => ((long)(uint)cx << 32) | (uint)cy;

        List<int> PopPool()
        {
            var l = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
            return l;
        }
    }
}
