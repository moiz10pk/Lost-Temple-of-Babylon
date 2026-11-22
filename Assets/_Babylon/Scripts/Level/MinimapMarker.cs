using UnityEngine;

namespace Babylon
{
    /// <summary>Keeps a MinimapIcon-layer quad above its target at a fixed height.</summary>
    public class MinimapMarker : MonoBehaviour
    {
        public Transform target;
        public float height = 5.2f;
        public bool rotateWithTarget;
        public bool hideWhenTargetInactive = true;
        Renderer[] rends;

        void Awake() => rends = GetComponentsInChildren<Renderer>();

        void LateUpdate()
        {
            bool show = target != null && (!hideWhenTargetInactive || target.gameObject.activeInHierarchy);
            foreach (var r in rends) r.enabled = show;
            if (!show) return;
            Vector3 p = target.position; p.y = height;
            transform.position = p;
            if (rotateWithTarget) transform.rotation = Quaternion.Euler(0, target.eulerAngles.y, 0);
        }
    }
}
