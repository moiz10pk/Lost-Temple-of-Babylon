using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>Where a creature may walk and how it gets from A to B (grid BFS in the maze, straight lines in open rooms).</summary>
    public abstract class WalkArea : MonoBehaviour
    {
        public abstract List<Vector3> FindPath(Vector3 from, Vector3 to);
        public abstract Vector3 RandomPatrolPoint();
    }
}
