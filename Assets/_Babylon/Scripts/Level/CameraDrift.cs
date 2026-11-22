using UnityEngine;

namespace Babylon
{
    /// <summary>Slow cinematic orbit for the main menu backdrop.</summary>
    public class CameraDrift : MonoBehaviour
    {
        public Vector3 target = new Vector3(0, 12, 0);
        public float radius = 75f;
        public float height = 14f;
        public float speed = 0.02f;
        public float startAngle = -110f;

        void Update()
        {
            float a = (startAngle + Mathf.Sin(Time.time * speed) * 18f) * Mathf.Deg2Rad;
            Vector3 p = target + new Vector3(Mathf.Cos(a) * radius, height + Mathf.Sin(Time.time * 0.13f) * 1.5f, Mathf.Sin(a) * radius);
            transform.position = p;
            transform.LookAt(target + Vector3.left * 12f);
        }
    }
}
