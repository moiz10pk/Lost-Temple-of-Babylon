using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>Serialized maze walls for runtime BFS pathing of the spiders (no NavMesh).</summary>
    public class MazeGrid : WalkArea
    {
        public int width = 12, height = 12;
        public float cellSize = 4f;
        public Vector3 origin;                    // world position of cell (0,0)'s south-west corner
        public bool[] wallE, wallN;

        MazeLayout layout;

        MazeLayout Layout
        {
            get
            {
                if (layout == null) layout = new MazeLayout { W = width, H = height, wallE = wallE, wallN = wallN };
                return layout;
            }
        }

        public void Store(MazeLayout m, float cell, Vector3 org)
        {
            width = m.W; height = m.H; cellSize = cell; origin = org;
            wallE = (bool[])m.wallE.Clone(); wallN = (bool[])m.wallN.Clone();
            layout = null;
        }

        public Vector2Int CellOf(Vector3 p)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / cellSize), 0, width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((p.z - origin.z) / cellSize), 0, height - 1);
            return new Vector2Int(x, y);
        }

        public Vector3 Center(Vector2Int c) => origin + new Vector3((c.x + 0.5f) * cellSize, 0, (c.y + 0.5f) * cellSize);

        public bool Inside(Vector3 p) =>
            p.x >= origin.x && p.z >= origin.z && p.x <= origin.x + width * cellSize && p.z <= origin.z + height * cellSize;

        public override List<Vector3> FindPath(Vector3 from, Vector3 to)
        {
            var cells = Layout.Path(CellOf(from), CellOf(to));
            var pts = new List<Vector3>();
            if (cells == null) return pts;
            for (int i = 1; i < cells.Count; i++) pts.Add(Center(cells[i]));
            if (Inside(to)) { Vector3 t = to; t.y = origin.y; pts.Add(t); }
            return pts;
        }

        public override Vector3 RandomPatrolPoint() => Center(new Vector2Int(Random.Range(0, width), Random.Range(0, height)));
    }
}
