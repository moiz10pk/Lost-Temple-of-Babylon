using UnityEngine;

namespace Babylon
{
    /// <summary>Spin and bob for pickups and glowing relics.</summary>
    public class SimpleMotion : MonoBehaviour
    {
        public Vector3 spin = new Vector3(0, 60, 0);
        public float bobHeight = 0.08f;
        public float bobSpeed = 2f;
        Vector3 basePos;

        void Start() => basePos = transform.localPosition;

        void Update()
        {
            transform.Rotate(spin * Time.deltaTime, Space.World);
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        }
    }
}
