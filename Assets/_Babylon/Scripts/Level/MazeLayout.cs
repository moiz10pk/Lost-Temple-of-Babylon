using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>
    /// Deterministic maze data: recursive backtracker, ~8% extra openings for loops (sparing dead ends),
    /// 2x2 side rooms, BFS distances, key room and urn spots. Pure data, shared by the builder and MazeGrid.
    /// </summary>
    public class MazeLayout
    {
        public int W, H;
        public bool[] wallE, wallN;               // wall on the east / north side of cell (x,y)
        public List<RectInt> rooms = new List<RectInt>();
        public List<Vector2Int> deadEnds = new List<Vector2Int>();
        public List<Vector2Int> urnCells = new List<Vector2Int>();
        public Vector2Int start, exit, keyCell;
        public int keyRoom;

        public int Idx(int x, int y) => y * W + x;
        static readonly Vector2Int[] Dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

        public bool CanMove(int x, int y, int d)
        {
            switch (d)
            {
                case 0: return y < H - 1 && !wallN[Idx(x, y)];
                case 1: return x < W - 1 && !wallE[Idx(x, y)];
                case 2: return y > 0 && !wallN[Idx(x, y - 1)];
                default: return x > 0 && !wallE[Idx(x - 1, y)];
            }
        }

        void Open(int x, int y, int d)
        {
            switch (d)
            {
                case 0: wallN[Idx(x, y)] = false; break;
                case 1: wallE[Idx(x, y)] = false; break;
                case 2: wallN[Idx(x, y - 1)] = false; break;
                default: wallE[Idx(x - 1, y)] = false; break;
            }
        }

        public int OpenSides(int x, int y) { int n = 0; for (int d = 0; d < 4; d++) if (CanMove(x, y, d)) n++; return n; }
        public bool InRoom(Vector2Int c) { foreach (var r in rooms) if (r.Contains(c)) return true; return false; }

        public static MazeLayout Generate(int w, int h, int seed, float loopFraction = 0.08f, int roomCount = 3)
        {
            var m = new MazeLayout { W = w, H = h, wallE = new bool[w * h], wallN = new bool[w * h] };
            for (int i = 0; i < w * h; i++) { m.wallE[i] = true; m.wallN[i] = true; }
            var rng = new System.Random(seed);
            m.start = new Vector2Int(0, 0); m.exit = new Vector2Int(w - 1, h - 1);

            // 1. Recursive backtracker.
            var visited = new bool[w * h];
            var stack = new Stack<Vector2Int>();
            stack.Push(m.start); visited[0] = true;
            var opts = new List<int>();
            while (stack.Count > 0)
            {
                var c = stack.Peek(); opts.Clear();
                for (int d = 0; d < 4; d++)
                {
                    var n = c + Dirs[d];
                    if (n.x >= 0 && n.y >= 0 && n.x < w && n.y < h && !visited[m.Idx(n.x, n.y)]) opts.Add(d);
                }
                if (opts.Count == 0) { stack.Pop(); continue; }
                int dir = opts[rng.Next(opts.Count)];
                m.Open(c.x, c.y, dir);
                var nx = c + Dirs[dir];
                visited[m.Idx(nx.x, nx.y)] = true;
                stack.Push(nx);
            }

            // 2. Side rooms (2x2), away from start/exit and each other.
            for (int tries = 0; tries < 500 && m.rooms.Count < roomCount; tries++)
            {
                var r = new RectInt(rng.Next(1, w - 2), rng.Next(1, h - 2), 2, 2);
                if (r.xMin < 4 && r.yMin < 4) continue;
                if (r.xMax > w - 3 && r.yMax > h - 3) continue;
                bool ok = true;
                foreach (var o in m.rooms)
                    if (new RectInt(o.xMin - 2, o.yMin - 2, o.width + 4, o.height + 4).Overlaps(r)) ok = false;
                if (!ok) continue;
                m.rooms.Add(r);
                m.wallE[m.Idx(r.xMin, r.yMin)] = false; m.wallE[m.Idx(r.xMin, r.yMin + 1)] = false;
                m.wallN[m.Idx(r.xMin, r.yMin)] = false; m.wallN[m.Idx(r.xMin + 1, r.yMin)] = false;
            }

            // 3. Loops: knock out a few interior walls, never next to a dead end.
            var candidates = new List<(int x, int y, int d)>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (x < w - 1 && m.wallE[m.Idx(x, y)]) candidates.Add((x, y, 1));
                    if (y < h - 1 && m.wallN[m.Idx(x, y)]) candidates.Add((x, y, 0));
                }
            int toRemove = Mathf.RoundToInt(candidates.Count * loopFraction);
            for (int i = 0; i < 2000 && toRemove > 0 && candidates.Count > 0; i++)
            {
                int k = rng.Next(candidates.Count);
                var (x, y, d) = candidates[k];
                candidates.RemoveAt(k);
                var n = new Vector2Int(x, y) + Dirs[d];
                if (m.OpenSides(x, y) == 1 || m.OpenSides(n.x, n.y) == 1) continue;
                m.Open(x, y, d); toRemove--;
            }

            // 4. Dead ends, key room (furthest by BFS), urn spots.
            var dist = m.Distances(m.start);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (c != m.start && c != m.exit && m.OpenSides(x, y) == 1 && !m.InRoom(c)) m.deadEnds.Add(c);
                }
            int best = -1;
            for (int i = 0; i < m.rooms.Count; i++)
            {
                var r = m.rooms[i];
                int d = dist[m.Idx(r.xMin, r.yMin)];
                if (d > best) { best = d; m.keyRoom = i; }
            }
            if (m.rooms.Count > 0) m.keyCell = new Vector2Int(m.rooms[m.keyRoom].xMin, m.rooms[m.keyRoom].yMin);
            for (int i = 0; i < m.rooms.Count; i++) if (i != m.keyRoom) m.urnCells.Add(new Vector2Int(m.rooms[i].xMin + 1, m.rooms[i].yMin + 1));
            m.deadEnds.Sort((a, b) => dist[m.Idx(b.x, b.y)].CompareTo(dist[m.Idx(a.x, a.y)]));
            for (int i = 0; i < m.deadEnds.Count && i < 4; i++) m.urnCells.Add(m.deadEnds[i * 2 < m.deadEnds.Count ? i * 2 : i]);
            return m;
        }

        public int[] Distances(Vector2Int from)
        {
            var dist = new int[W * H];
            for (int i = 0; i < dist.Length; i++) dist[i] = int.MaxValue;
            var q = new Queue<Vector2Int>();
            dist[Idx(from.x, from.y)] = 0; q.Enqueue(from);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    if (!CanMove(c.x, c.y, d)) continue;
                    var n = c + Dirs[d];
                    int ni = Idx(n.x, n.y);
                    if (dist[ni] != int.MaxValue) continue;
                    dist[ni] = dist[Idx(c.x, c.y)] + 1; q.Enqueue(n);
                }
            }
            return dist;
        }

        /// <summary>Cell path from a to b (inclusive) via BFS, or null if unreachable.</summary>
        public List<Vector2Int> Path(Vector2Int a, Vector2Int b)
        {
            var prev = new int[W * H];
            for (int i = 0; i < prev.Length; i++) prev[i] = -1;
            var q = new Queue<Vector2Int>();
            prev[Idx(a.x, a.y)] = Idx(a.x, a.y); q.Enqueue(a);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == b) break;
                for (int d = 0; d < 4; d++)
                {
                    if (!CanMove(c.x, c.y, d)) continue;
                    var n = c + Dirs[d];
                    if (prev[Idx(n.x, n.y)] != -1) continue;
                    prev[Idx(n.x, n.y)] = Idx(c.x, c.y); q.Enqueue(n);
                }
            }
            if (prev[Idx(b.x, b.y)] == -1) return null;
            var path = new List<Vector2Int>();
            int cur = Idx(b.x, b.y);
            while (cur != Idx(a.x, a.y)) { path.Add(new Vector2Int(cur % W, cur / W)); cur = prev[cur]; }
            path.Add(a);
            path.Reverse();
            return path;
        }
    }
}
