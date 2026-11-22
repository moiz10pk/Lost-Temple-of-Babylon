using UnityEditor;
using UnityEngine;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>Builds prop and FX prefabs. Graphics live under a child named "Visual"; scripts/colliders on the root.</summary>
    public static class PropPrefabs
    {
        public const string Dir = "Assets/_Babylon/Generated/Prefabs";
        static readonly Color TorchLight = new Color32(0xFF, 0xB7, 0x66, 255);

        public static GameObject Save(GameObject go, string name)
        {
            System.IO.Directory.CreateDirectory(Dir);
            var p = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{name}.prefab");
            Object.DestroyImmediate(go);
            Lib.SetPrefab(name, p);
            return p;
        }

        // ---------------- particles ----------------
        public static ParticleSystem Particles(Transform parent, string name, string mat, bool loop, float rate, int burst, float life, float speed,
            float size, Color c0, Color c1, float gravity, ParticleSystemShapeType shape, float radius, bool stretch = false, float angle = 25f)
        {
            var go = Node(name, parent, Vector3.zero);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = loop; main.duration = loop ? 2f : 1f; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(c0, c1);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 600;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var em = ps.emission; em.rateOverTime = rate;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var sh = ps.shape; sh.shapeType = shape; sh.radius = radius; sh.angle = angle;
            if (shape == ParticleSystemShapeType.Box) sh.scale = new Vector3(radius, radius * 0.5f, radius);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.6f, 1, 1.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = M(mat);
            r.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (stretch) { r.velocityScale = 0.04f; r.lengthScale = 1.5f; }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        public static void Fire(Transform parent, Vector3 pos, float scale = 1f)
        {
            var f = Particles(parent, "FireFX", "P_Fire", true, 28, 0, 0.7f, 1.2f, 0.35f * scale, new Color(1, 0.6f, 0.2f), new Color(1, 0.35f, 0.1f), -0.25f, ParticleSystemShapeType.Cone, 0.08f * scale, false, 12);
            f.transform.localPosition = pos;
            f.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var s = Particles(f.transform, "Smoke", "P_Smoke", true, 6, 0, 2.2f, 0.9f, 0.6f * scale, new Color(0.2f, 0.18f, 0.16f, 0.4f), new Color(0.1f, 0.1f, 0.1f, 0.3f), -0.05f, ParticleSystemShapeType.Cone, 0.05f, false, 8);
            s.transform.localPosition = new Vector3(0, 0, 0.3f * scale);
            var e = Particles(f.transform, "Embers", "P_Spark", true, 4, 0, 1.6f, 1.6f, 0.04f, new Color(1, 0.7f, 0.3f), new Color(1, 0.4f, 0.1f), -0.1f, ParticleSystemShapeType.Cone, 0.1f, false, 25);
            e.transform.localPosition = Vector3.zero;
        }

        static void FxPrefab(string name, string mat, int burst, float life, float speed, float size, Color c0, Color c1, float gravity,
            ParticleSystemShapeType shape, float radius, bool stretch = false, bool loop = false, float rate = 0)
        {
            var root = new GameObject(name);
            var ps = Particles(root.transform, "Particles", mat, loop, rate, burst, life, speed, size, c0, c1, gravity, shape, radius, stretch, 35);
            ps.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            Save(root, name);
        }

        public static void BuildFx()
        {
            FxPrefab("FX_Dust", "P_Dust", 45, 1.8f, 4.5f, 1.4f, new Color(0.75f, 0.63f, 0.45f, 0.6f), new Color(0.55f, 0.45f, 0.32f, 0.5f), 0.15f, ParticleSystemShapeType.Hemisphere, 0.8f);
            FxPrefab("FX_Sparks", "P_Spark", 32, 0.5f, 8f, 0.07f, new Color(1, 0.85f, 0.5f), new Color(1, 0.5f, 0.2f), 1.2f, ParticleSystemShapeType.Cone, 0.05f, true);
            FxPrefab("FX_Blood", "P_Blood", 28, 0.8f, 3.5f, 0.22f, new Color(0.15f, 0.25f, 0.05f, 0.9f), new Color(0.05f, 0.1f, 0.02f, 0.9f), 1.4f, ParticleSystemShapeType.Sphere, 0.2f);
            FxPrefab("FX_Heal", "P_Heal", 40, 1.4f, 1.6f, 0.15f, new Color(0.3f, 1, 0.7f), new Color(0.6f, 1, 0.9f), -0.4f, ParticleSystemShapeType.Sphere, 0.4f);
            FxPrefab("FX_CoreHit", "P_Core", 40, 0.6f, 7f, 0.12f, new Color(0.4f, 0.9f, 1f), new Color(0.2f, 0.6f, 1f), 0.5f, ParticleSystemShapeType.Sphere, 0.2f, true);
            FxPrefab("FX_LightBurst", "P_Mote", 120, 1.8f, 7f, 0.3f, new Color(1, 0.9f, 0.6f), new Color(1, 0.75f, 0.35f), -0.1f, ParticleSystemShapeType.Sphere, 0.3f);
            FxPrefab("FX_DustMotes", "P_Mote", 0, 9f, 0.12f, 0.06f, new Color(1, 0.85f, 0.55f, 0.6f), new Color(1, 0.95f, 0.8f, 0.4f), -0.005f, ParticleSystemShapeType.Box, 14f, false, true, 18);
            FxPrefab("FX_FallingDust", "P_Dust", 0, 4f, 0.2f, 0.35f, new Color(0.6f, 0.55f, 0.45f, 0.35f), new Color(0.45f, 0.4f, 0.35f, 0.25f), 0.08f, ParticleSystemShapeType.Box, 12f, false, true, 10);
        }

        // ---------------- props ----------------
        public static void BuildTorches()
        {
            var root = new GameObject("Torch");
            var vis = Node("Visual", root.transform, Vector3.zero).transform;
            Cyl(vis, "Base", new Vector3(0, 0.06f, 0), 0.22f, 0.12f, "Bronze");
            Cyl(vis, "Pole", new Vector3(0, 1.0f, 0), 0.045f, 1.9f, "Wood", false, null, 10);
            Cyl(vis, "Bowl", new Vector3(0, 2.02f, 0), 0.1f, 0.26f, "Bronze", false, null, 16, 0.26f);
            Prim(PrimitiveType.Sphere, vis, "Coals", new Vector3(0, 2.13f, 0), new Vector3(0.42f, 0.1f, 0.42f), "Flame");
            var flame = Prim(PrimitiveType.Sphere, vis, "Flame", new Vector3(0, 2.36f, 0), new Vector3(0.24f, 0.5f, 0.24f), "Flame");
            flame.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Fire(vis, new Vector3(0, 2.15f, 0));
            var l = PointLight(root.transform, "Light", new Vector3(0, 2.55f, 0), TorchLight, 2.4f, 11f);
            var fl = root.AddComponent<TorchFlicker>(); fl.torchLight = l; fl.flame = flame.transform; fl.baseIntensity = 2.4f;
            var c = root.AddComponent<CapsuleCollider>(); c.radius = 0.18f; c.height = 2.3f; c.center = new Vector3(0, 1.15f, 0);
            Save(root, "Torch");

            root = new GameObject("WallTorch");
            vis = Node("Visual", root.transform, Vector3.zero).transform;
            Box(vis, "Plate", new Vector3(0, 0, 0.03f), new Vector3(0.22f, 0.4f, 0.06f), "Bronze", false, 0.5f);
            Box(vis, "Arm", new Vector3(0, 0, 0.2f), new Vector3(0.06f, 0.06f, 0.34f), "Bronze", false, 0.5f);
            Cyl(vis, "Cup", new Vector3(0, 0.1f, 0.38f), 0.06f, 0.2f, "Bronze", false, null, 12, 0.14f);
            flame = Prim(PrimitiveType.Sphere, vis, "Flame", new Vector3(0, 0.33f, 0.38f), new Vector3(0.17f, 0.36f, 0.17f), "Flame");
            flame.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Fire(vis, new Vector3(0, 0.2f, 0.38f), 0.75f);
            l = PointLight(root.transform, "Light", new Vector3(0, 0.45f, 0.7f), TorchLight, 2.2f, 9f);
            fl = root.AddComponent<TorchFlicker>(); fl.torchLight = l; fl.flame = flame.transform; fl.baseIntensity = 2.2f;
            Save(root, "WallTorch");
        }

        public static void BuildBrazier()
        {
            var root = new GameObject("Brazier");
            var vis = Node("Visual", root.transform, Vector3.zero).transform;
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var leg = Cyl(vis, "Leg" + i, Quaternion.Euler(0, a, 0) * new Vector3(0, 0.5f, 0.28f), 0.04f, 1.1f, "Bronze", false, null, 8);
                leg.transform.localRotation = Quaternion.Euler(0, a, 0) * Quaternion.Euler(-14, 0, 0);
            }
            Cyl(vis, "Bowl", new Vector3(0, 1.08f, 0), 0.25f, 0.3f, "Bronze", false, null, 20, 0.52f);
            Cyl(vis, "Rim", new Vector3(0, 1.24f, 0), 0.54f, 0.04f, "Gold", false, null, 24);
            var fire = Node("Fire", root.transform, Vector3.zero);
            Prim(PrimitiveType.Sphere, fire.transform, "Coals", new Vector3(0, 1.22f, 0), new Vector3(0.85f, 0.15f, 0.85f), "Flame");
            Fire(fire.transform, new Vector3(0, 1.25f, 0), 1.8f);
            var l = PointLight(fire.transform, "Light", new Vector3(0, 1.8f, 0), new Color(1f, 0.62f, 0.3f), 3.2f, 12f);
            var fl = fire.AddComponent<TorchFlicker>(); fl.torchLight = l; fl.baseIntensity = 3.2f; fl.crackleSound = false;
            var b = root.AddComponent<Brazier>(); b.fire = fire; b.fireLight = l; b.litOnStart = true;
            var c = root.AddComponent<BoxCollider>(); c.size = new Vector3(1.1f, 1.3f, 1.1f); c.center = new Vector3(0, 0.65f, 0);
            Save(root, "Brazier");
        }

        public static void BuildPickups()
        {
            // Healing urn
            var root = new GameObject("HealingUrn");
            root.layer = Layers.Interactable;
            var vis = Node("Visual", root.transform, Vector3.zero).transform;
            Prim(PrimitiveType.Sphere, vis, "Body", new Vector3(0, 0.45f, 0), new Vector3(0.62f, 0.72f, 0.62f), "Clay");
            Cyl(vis, "Band", new Vector3(0, 0.52f, 0), 0.315f, 0.06f, "Gold", false, null, 24);
            Cyl(vis, "Neck", new Vector3(0, 0.86f, 0), 0.15f, 0.26f, "Clay", false, null, 16, 0.12f);
            Cyl(vis, "Lip", new Vector3(0, 0.99f, 0), 0.19f, 0.05f, "Clay", false, null, 16);
            var glow = Prim(PrimitiveType.Sphere, vis, "Glow", new Vector3(0, 1.0f, 0), new Vector3(0.24f, 0.05f, 0.24f), "HealGlow");
            var l = PointLight(root.transform, "Light", new Vector3(0, 1.3f, 0), new Color(0.3f, 1f, 0.75f), 1.6f, 4.5f);
            var urn = root.AddComponent<HealingUrn>(); urn.verb = "drink from the healing urn"; urn.glowLight = l; urn.glowRenderer = glow.GetComponent<Renderer>();
            var c = root.AddComponent<BoxCollider>(); c.size = new Vector3(0.7f, 1.05f, 0.7f); c.center = new Vector3(0, 0.52f, 0);
            Save(root, "HealingUrn");

            // Golden key
            root = new GameObject("GoldenKey");
            root.layer = Layers.Interactable;
            vis = Node("Visual", root.transform, new Vector3(0, 0.15f, 0)).transform;
            var key = Node("Key", vis, Vector3.zero, new Vector3(0, 0, 90)).transform;
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Box(key, "Ring" + i, new Vector3(Mathf.Cos(a) * 0.09f, 0, Mathf.Sin(a) * 0.09f - 0.2f), new Vector3(0.05f, 0.03f, 0.05f), "GoldBright", false, 0.2f, new Vector3(0, -i * 30f, 0));
            }
            Box(key, "Shaft", new Vector3(0, 0, 0.05f), new Vector3(0.035f, 0.035f, 0.42f), "GoldBright", false, 0.2f);
            Box(key, "Tooth1", new Vector3(0.05f, 0, 0.18f), new Vector3(0.09f, 0.03f, 0.035f), "GoldBright", false, 0.2f);
            Box(key, "Tooth2", new Vector3(0.05f, 0, 0.24f), new Vector3(0.07f, 0.03f, 0.035f), "GoldBright", false, 0.2f);
            var m = vis.gameObject.AddComponent<SimpleMotion>(); m.spin = new Vector3(0, 70, 0); m.bobHeight = 0.06f;
            PointLight(root.transform, "Light", new Vector3(0, 0.4f, 0), new Color(1f, 0.82f, 0.4f), 1.4f, 3.5f);
            var kp = root.AddComponent<KeyPickup>(); kp.verb = "take the Golden Key";
            c = root.AddComponent<BoxCollider>(); c.size = new Vector3(0.6f, 0.45f, 0.6f); c.center = new Vector3(0, 0.15f, 0);
            Save(root, "GoldenKey");

            // Clay clue tablet
            root = new GameObject("ClayTablet");
            root.layer = Layers.Interactable;
            vis = Node("Visual", root.transform, Vector3.zero).transform;
            Box(vis, "Tablet", new Vector3(0, 0.035f, 0), new Vector3(0.5f, 0.07f, 0.36f), "Clay", false, 0.5f, new Vector3(0, 8, 0));
            var ct = root.AddComponent<ClueTablet>(); ct.verb = "read the clay tablet";
            c = root.AddComponent<BoxCollider>(); c.size = new Vector3(0.6f, 0.25f, 0.45f); c.center = new Vector3(0, 0.1f, 0);
            Save(root, "ClayTablet");

            // Sumerian tablet (the prize)
            root = new GameObject("SumerianTablet");
            root.layer = Layers.Interactable;
            vis = Node("Visual", root.transform, new Vector3(0, 0.45f, 0)).transform;
            Box(vis, "Tablet", Vector3.zero, new Vector3(0.55f, 0.75f, 0.1f), "ClayGlow", false, 0.8f);
            Box(vis, "Edge", new Vector3(0, 0, -0.01f), new Vector3(0.6f, 0.8f, 0.08f), "Clay", false, 0.8f);
            m = vis.gameObject.AddComponent<SimpleMotion>(); m.spin = new Vector3(0, 18, 0); m.bobHeight = 0.05f;
            l = PointLight(root.transform, "Light", new Vector3(0, 0.6f, 0.4f), new Color(1f, 0.78f, 0.4f), 3f, 7f);
            var st = root.AddComponent<SumerianTablet>(); st.verb = "Collect"; st.tabletLight = l;
            c = root.AddComponent<BoxCollider>(); c.size = new Vector3(0.8f, 1f, 0.6f); c.center = new Vector3(0, 0.45f, 0);
            var motes = Particles(root.transform, "Motes", "P_Mote", true, 10, 0, 3f, 0.3f, 0.05f, new Color(1, 0.85f, 0.5f), new Color(1, 0.95f, 0.7f), -0.05f, ParticleSystemShapeType.Sphere, 0.7f);
            motes.transform.localPosition = new Vector3(0, 0.5f, 0);
            Save(root, "SumerianTablet");
        }

        /// <summary>Bronze sword with gold guard and lapis pommel, blade along +Y, grip at the origin.</summary>
        public static GameObject BuildSword(Transform parent)
        {
            var s = Node("Sword", parent, Vector3.zero).transform;
            Cyl(s, "Grip", Vector3.zero, 0.022f, 0.17f, "Leather", false, null, 10);
            Prim(PrimitiveType.Sphere, s, "Pommel", new Vector3(0, -0.1f, 0), Vector3.one * 0.06f, "LapisPlain");
            Box(s, "Guard", new Vector3(0, 0.095f, 0), new Vector3(0.22f, 0.035f, 0.05f), "Gold", false, 0.2f);
            var blade = Box(s, "Blade", new Vector3(0, 0.5f, 0), new Vector3(0.055f, 0.78f, 0.012f), "BronzeBlade", false, 0.5f);
            Box(s, "Tip", new Vector3(0, 0.89f, 0), new Vector3(0.039f, 0.039f, 0.012f), "BronzeBlade", false, 0.5f, new Vector3(0, 0, 45));
            Box(s, "Fuller", new Vector3(0, 0.46f, 0), new Vector3(0.012f, 0.62f, 0.014f), "GoldBright", false, 0.5f);
            foreach (var r in s.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return blade;
        }

        public static void BuildSwordPrefab()
        {
            var root = new GameObject("Sword");
            var vis = Node("Visual", root.transform, Vector3.zero).transform;
            BuildSword(vis);
            Save(root, "Sword");
        }

        public static void BuildLamassu()
        {
            var root = new GameObject("Lamassu");
            var v = Node("Visual", root.transform, Vector3.zero).transform;
            Box(v, "Plinth", new Vector3(0, 0.2f, 0), new Vector3(2.2f, 0.4f, 3.8f), "SandstoneDark", false);
            Box(v, "Body", new Vector3(0, 1.65f, -0.1f), new Vector3(1.3f, 1.25f, 2.6f), "CarvedWall", false, 1.5f);
            Box(v, "Chest", new Vector3(0, 1.8f, 1.3f), new Vector3(1.3f, 1.2f, 0.6f), "CarvedWall", false, 1.5f);
            float[] lz = { 1.25f, 0.85f, -0.95f, -1.25f };
            for (int i = 0; i < 4; i++)
                for (int s = -1; s <= 1; s += 2)
                {
                    Cyl(v, $"Leg{i}{s}", new Vector3(s * 0.45f, 0.95f, lz[i]), 0.2f, 1.1f, "Sandstone", false, null, 10);
                    Box(v, $"Hoof{i}{s}", new Vector3(s * 0.45f, 0.48f, lz[i] + 0.05f), new Vector3(0.36f, 0.16f, 0.42f), "SandstoneDark", false, 0.5f);
                }
            for (int s = -1; s <= 1; s += 2)
            {
                Box(v, "Wing" + s, new Vector3(s * 0.68f, 2.55f, -0.3f), new Vector3(0.12f, 1.4f, 2.6f), "CarvedWall", false, 1f, new Vector3(-18, 0, s * -12));
                Box(v, "WingTip" + s, new Vector3(s * 0.8f, 3.3f, -1.2f), new Vector3(0.1f, 0.8f, 1.2f), "CarvedWall", false, 1f, new Vector3(-35, 0, s * -15));
            }
            Box(v, "Neck", new Vector3(0, 2.4f, 1.55f), new Vector3(0.8f, 0.8f, 0.7f), "Sandstone", false);
            Box(v, "Head", new Vector3(0, 2.95f, 1.8f), new Vector3(0.72f, 0.8f, 0.7f), "Sandstone", false);
            Box(v, "Beard", new Vector3(0, 2.45f, 2.08f), new Vector3(0.62f, 0.85f, 0.26f), "CarvedWall", false, 0.6f);
            Box(v, "Brow", new Vector3(0, 3.15f, 2.12f), new Vector3(0.7f, 0.12f, 0.1f), "SandstoneDark", false, 0.5f);
            Cyl(v, "Crown", new Vector3(0, 3.7f, 1.75f), 0.36f, 0.75f, "Sandstone", false, null, 20, 0.4f);
            for (int i = 0; i < 3; i++) Cyl(v, "Horn" + i, new Vector3(0, 3.42f + i * 0.22f, 1.75f), 0.39f + i * 0.012f, 0.06f, "Gold", false, null, 20);
            Cyl(v, "Tail", new Vector3(0, 1.4f, -1.55f), 0.07f, 1.1f, "Sandstone", false, new Vector3(20, 0, 0), 8);
            var c = root.AddComponent<BoxCollider>(); c.size = new Vector3(2.2f, 4.1f, 3.8f); c.center = new Vector3(0, 2.05f, 0);
            Save(root, "Lamassu");
        }

        public static void BuildPalm()
        {
            var root = new GameObject("DatePalm");
            var v = Node("Visual", root.transform, Vector3.zero).transform;
            var rnd = new System.Random(5);
            Vector3 p = Vector3.zero; float lean = 0;
            for (int i = 0; i < 9; i++)
            {
                lean += 1.6f;
                var seg = Cyl(v, "Trunk" + i, p + new Vector3(0, 0.42f, 0), 0.23f - i * 0.006f, 0.86f, "PalmTrunk", false, new Vector3(0, 0, -lean), 12, 0.19f - i * 0.006f, 1f);
                Cyl(seg.transform, "Ring", new Vector3(0, -0.4f, 0), 0.25f - i * 0.006f, 0.07f, "PalmTrunk", false, null, 12);
                p += Quaternion.Euler(0, 0, -lean) * Vector3.up * 0.84f;
            }
            var crown = Node("Crown", v, p).transform;
            for (int i = 0; i < 14; i++)
            {
                float yaw = i * (360f / 14f) + (float)rnd.NextDouble() * 10f;
                float pitch = -25f + (float)rnd.NextDouble() * 30f;
                float sc = 0.85f + (float)rnd.NextDouble() * 0.35f;
                var leaf = Mesh(crown, "Leaf" + i, MeshGen.Leaf(), "PalmLeaf", Vector3.zero, new Vector3(pitch, yaw, 0), Vector3.one * sc);
            }
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                Prim(PrimitiveType.Sphere, crown, "Dates" + i, new Vector3(Mathf.Cos(a) * 0.35f, -0.35f, Mathf.Sin(a) * 0.35f), new Vector3(0.3f, 0.45f, 0.3f), "Clay");
            }
            var c = root.AddComponent<CapsuleCollider>(); c.radius = 0.3f; c.height = 7f; c.center = new Vector3(0.3f, 3.5f, 0);
            Save(root, "DatePalm");
        }

        public static void BuildRocks()
        {
            for (int i = 1; i <= 3; i++)
            {
                var root = new GameObject("Rock" + i);
                Mesh(Node("Visual", root.transform, Vector3.zero).transform, "Rock", MeshGen.Rock(i * 7), "Sandstone", new Vector3(0, 0.3f, 0));
                var c = root.AddComponent<SphereCollider>(); c.radius = 0.6f; c.center = new Vector3(0, 0.3f, 0);
                Save(root, "Rock" + i);
            }
        }

        /// <summary>Lapis column with gold capital. Height is the shaft height.</summary>
        public static GameObject BuildPillar(Transform parent, string name, Vector3 pos, float height, string shaftMat = "LapisTile")
        {
            var root = Node(name, parent, pos);
            var v = Node("Visual", root.transform, Vector3.zero).transform;
            Box(v, "Base", new Vector3(0, 0.2f, 0), new Vector3(1.3f, 0.4f, 1.3f), "Sandstone", false, 1f);
            Cyl(v, "Torus", new Vector3(0, 0.5f, 0), 0.58f, 0.2f, "Gold", false, null, 24);
            Cyl(v, "Shaft", new Vector3(0, 0.4f + height * 0.5f, 0), 0.47f, height, shaftMat, false, null, 24, 0.43f, 1.5f);
            Cyl(v, "Neck", new Vector3(0, 0.45f + height, 0), 0.5f, 0.15f, "Gold", false, null, 24);
            Cyl(v, "Capital", new Vector3(0, 0.75f + height, 0), 0.45f, 0.5f, "Gold", false, null, 24, 0.75f);
            Box(v, "Abacus", new Vector3(0, 1.1f + height, 0), new Vector3(1.6f, 0.25f, 1.6f), "Gold", false, 1f);
            var c = root.AddComponent<CapsuleCollider>(); c.radius = 0.55f; c.height = height + 1.3f; c.center = new Vector3(0, (height + 1.3f) * 0.5f, 0);
            return root;
        }

        public static void BuildPillars()
        {
            Save(BuildPillar(null, "Pillar", Vector3.zero, 7f), "Pillar");

            // Sturdy boss-arena pillar (tag Pillar) with lapis tiles, moss bands and a wall torch.
            var root = new GameObject("StonePillar");
            root.tag = Layers.TagPillar;
            var v = Node("Visual", root.transform, Vector3.zero).transform;
            Box(v, "Plinth", new Vector3(0, 0.35f, 0), new Vector3(2.3f, 0.7f, 2.3f), "MossRock", false, 1.5f);
            Cyl(v, "Shaft", new Vector3(0, 5.2f, 0), 0.85f, 9f, "LapisTile", false, null, 28, 0.8f, 1.5f);
            Cyl(v, "BandLow", new Vector3(0, 1.4f, 0), 0.92f, 0.5f, "MossRock", false, null, 28);
            Cyl(v, "BandHigh", new Vector3(0, 7.5f, 0), 0.9f, 0.4f, "MossRock", false, null, 28);
            Cyl(v, "Gold", new Vector3(0, 9.6f, 0), 0.95f, 0.25f, "Gold", false, null, 28);
            Box(v, "Cap", new Vector3(0, 10.1f, 0), new Vector3(2.2f, 0.8f, 2.2f), "MossRock", false, 1.5f);
            var t = Place("WallTorch", root.transform, new Vector3(0, 3.2f, 0.86f));
            var c = root.AddComponent<CapsuleCollider>(); c.radius = 0.95f; c.height = 10.5f; c.center = new Vector3(0, 5.25f, 0);
            Save(root, "StonePillar");
        }

        public static void BuildCombatProps()
        {
            var root = new GameObject("Boulder");
            Mesh(Node("Visual", root.transform, Vector3.zero).transform, "Rock", MeshGen.Rock(99), "MossRock", Vector3.zero, null, Vector3.one * 0.9f);
            root.AddComponent<Boulder>();
            Save(root, "Boulder");

            root = new GameObject("Shockwave");
            var ring = Mesh(Node("Visual", root.transform, Vector3.zero).transform, "Ring", MeshGen.Ring(), "ShockRing", Vector3.up * 0.05f);
            ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.AddComponent<Shockwave>().ringRenderer = ring.GetComponent<MeshRenderer>();
            Save(root, "Shockwave");

            foreach (var (name, deg) in new[] { ("Decal_Ring", 360f), ("Decal_Arc", 170f) })
            {
                root = new GameObject(name);
                var d = Mesh(Node("Visual", root.transform, Vector3.zero).transform, "Decal", MeshGen.Fan(deg), "DecalRed", Vector3.zero);
                d.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                root.AddComponent<TelegraphDecal>().rend = d.GetComponent<MeshRenderer>();
                Save(root, name);
            }
        }

        /// <summary>Gold-framed lion relief panel; the 'bowed' variant lowers its head and shows faint glowing cuneiform.</summary>
        public static GameObject BuildLionRelief(Transform parent, string name, Vector3 pos, float yaw, bool bowed)
        {
            var root = Node(name, parent, pos, new Vector3(0, yaw, 0));
            root.layer = Layers.Interactable;
            var v = Node("Visual", root.transform, Vector3.zero).transform;
            Box(v, "Frame", new Vector3(0, 0, 0.05f), new Vector3(2.4f, 1.6f, 0.1f), "Gold", false, 1f);
            Box(v, "Panel", new Vector3(0, 0, 0.11f), new Vector3(2.2f, 1.4f, 0.04f), "LapisTile", false, 1.4f);
            Box(v, "Body", new Vector3(-0.1f, -0.05f, 0.17f), new Vector3(1.1f, 0.45f, 0.1f), "GoldBright", false, 0.5f);
            Box(v, "Mane", new Vector3(0.45f, 0.08f, 0.18f), new Vector3(0.4f, 0.55f, 0.1f), "Gold", false, 0.5f);
            var head = Node("HeadPivot", v, new Vector3(0.55f, 0.15f, 0.2f), new Vector3(0, 0, bowed ? -38 : 0)).transform;
            Box(head, "Head", new Vector3(0.2f, bowed ? -0.05f : 0.05f, 0), new Vector3(0.32f, 0.3f, 0.1f), "GoldBright", false, 0.5f);
            Box(head, "Jaw", new Vector3(0.32f, -0.08f, 0), new Vector3(0.14f, 0.12f, 0.1f), "Gold", false, 0.5f);
            for (int i = 0; i < 4; i++)
                Box(v, "Leg" + i, new Vector3(-0.5f + i * 0.3f + (i > 1 ? 0.1f : 0), -0.42f, 0.17f), new Vector3(0.1f, 0.35f, 0.08f), "GoldBright", false, 0.5f);
            Box(v, "Tail", new Vector3(-0.75f, 0.1f, 0.17f), new Vector3(0.35f, 0.06f, 0.06f), "Gold", false, 0.5f, new Vector3(0, 0, 40));
            if (bowed)
            {
                var glyphs = Mesh(v, "Glyphs", MeshGen.Quad(2.0f, 0.3f, 2f), "CuneiformFaint", new Vector3(0, -0.58f, 0.135f), new Vector3(0, 180, 0));
                PointLight(root.transform, "GlyphLight", new Vector3(0, -0.4f, 0.6f), new Color(0.25f, 0.82f, 1f), 0.5f, 2.5f);
            }
            var c = root.AddComponent<BoxCollider>(); c.size = new Vector3(2.4f, 1.6f, 0.4f); c.center = new Vector3(0, 0, 0.15f);
            var lr = root.AddComponent<LionRelief>(); lr.isCorrect = bowed; lr.verb = "press the lion relief";
            return root;
        }

        public static void BuildAll()
        {
            BuildFx(); BuildTorches(); BuildBrazier(); BuildPickups(); BuildSwordPrefab(); BuildLamassu(); BuildPalm();
            BuildRocks(); BuildPillars(); BuildCombatProps();
            Save(BuildLionRelief(null, "LionRelief", Vector3.zero, 0, false), "LionRelief");
        }
    }
}
