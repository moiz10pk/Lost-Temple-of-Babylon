using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>An open room: patrol inside one box, hunt anywhere inside a larger one (straight-line steering).</summary>
    public class OpenArea : WalkArea
    {
        public Vector3 patrolCenter, patrolSize = new Vector3(8, 0, 8);
        public Vector3 roamCenter, roamSize = new Vector3(20, 0, 20);

        Vector3 Clamp(Vector3 p, Vector3 c, Vector3 s)
        {
            Vector3 w = transform.TransformPoint(c);
            p.x = Mathf.Clamp(p.x, w.x - s.x * 0.5f, w.x + s.x * 0.5f);
            p.z = Mathf.Clamp(p.z, w.z - s.z * 0.5f, w.z + s.z * 0.5f);
            return p;
        }

        public override List<Vector3> FindPath(Vector3 from, Vector3 to) => new List<Vector3> { Clamp(to, roamCenter, roamSize) };

        public override Vector3 RandomPatrolPoint()
        {
            Vector3 w = transform.TransformPoint(patrolCenter);
            return new Vector3(w.x + Random.Range(-0.5f, 0.5f) * patrolSize.x, w.y, w.z + Random.Range(-0.5f, 0.5f) * patrolSize.z);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow; Gizmos.DrawWireCube(transform.TransformPoint(patrolCenter), patrolSize + Vector3.up);
            Gizmos.color = Color.red; Gizmos.DrawWireCube(transform.TransformPoint(roamCenter), roamSize + Vector3.up);
        }
    }
}
