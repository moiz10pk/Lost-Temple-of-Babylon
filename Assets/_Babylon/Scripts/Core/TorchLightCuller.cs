using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>Keeps only the nearest torch lights enabled (Forward+ handles many, this keeps it cheap on laptops).</summary>
    public class TorchLightCuller : MonoBehaviour
    {
        public int maxLights = 14;
        static readonly List<Light> torches = new List<Light>();
        float timer;

        public static void Register(Light l) { if (l && !torches.Contains(l)) torches.Add(l); }
        public static void Unregister(Light l) => torches.Remove(l);

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0) return;
            timer = 0.25f;
            var cam = Camera.main;
            if (!cam) return;
            Vector3 p = cam.transform.position;
            torches.RemoveAll(l => l == null);
            torches.Sort((a, b) => (a.transform.position - p).sqrMagnitude.CompareTo((b.transform.position - p).sqrMagnitude));
            for (int i = 0; i < torches.Count; i++) torches[i].enabled = i < maxLights;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void DomainReset() => torches.Clear();
    }
}
