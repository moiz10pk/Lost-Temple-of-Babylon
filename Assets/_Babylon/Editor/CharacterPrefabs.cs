using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>Player, giant spider, scorpion and the Stone Master, all built from primitives under "Visual".</summary>
    public static class CharacterPrefabs
    {
        public static void BuildAll()
        {
            BuildPlayer(); BuildSpider(); BuildScorpion(); BuildStoneMaster();
        }

        // ---------------------------------------------------------------- Player
        static void BuildPlayer()
        {
            var root = new GameObject("Player");
            root.tag = Layers.TagPlayer;
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.4f; cc.slopeLimit = 45f; cc.skinWidth = 0.05f;
            var hp = root.AddComponent<Health>(); hp.maxHealth = 100f; hp.iFrameTime = 0.35f;
            root.AddComponent<PlayerVitals>();
            root.AddComponent<PlayerMotor>();
            var look = root.AddComponent<PlayerLook>();
            var inter = root.AddComponent<Interactor>();
            var sword = root.AddComponent<SwordWeapon>();

            // Explorer body (shadow only, so the first-person view stays clean).
            var body = Node("Visual", root.transform, Vector3.zero).transform;
            Prim(PrimitiveType.Capsule, body, "Tunic", new Vector3(0, 1.0f, 0), new Vector3(0.62f, 0.55f, 0.42f), "Linen");
            Prim(PrimitiveType.Cylinder, body, "Belt", new Vector3(0, 0.88f, 0), new Vector3(0.6f, 0.05f, 0.44f), "Leather");
            Prim(PrimitiveType.Sphere, body, "Head", new Vector3(0, 1.62f, 0), new Vector3(0.28f, 0.3f, 0.28f), "Skin");
            Prim(PrimitiveType.Sphere, body, "Headwrap", new Vector3(0, 1.7f, -0.02f), new Vector3(0.31f, 0.2f, 0.32f), "Linen");
            for (int s = -1; s <= 1; s += 2) Prim(PrimitiveType.Capsule, body, "Leg" + s, new Vector3(s * 0.13f, 0.4f, 0), new Vector3(0.18f, 0.42f, 0.18f), "Linen");
            foreach (var r in body.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            var rig = Node("CameraRig", root.transform, new Vector3(0, 1.64f, 0)).transform;
            var bob = Node("BobPivot", rig, Vector3.zero).transform;
            var camGo = Node("Main Camera", bob, Vector3.zero);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f; cam.farClipPlane = 600f; cam.fieldOfView = 70f;
            cam.cullingMask = ~LayerMask.GetMask(Layers.MinimapName);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraShake>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            // View model: bronze sword in an adventurer's bracer-wrapped hand.
            var pivot = Node("SwordPivot", camGo.transform, new Vector3(0.38f, -0.36f, 0.62f), new Vector3(8, -12, -4)).transform;
            var vm = Node("Visual", pivot, Vector3.zero).transform;
            Prim(PrimitiveType.Sphere, vm, "Hand", new Vector3(0, 0, 0), new Vector3(0.1f, 0.09f, 0.12f), "Skin");
            Prim(PrimitiveType.Cylinder, vm, "Bracer", new Vector3(0.02f, -0.06f, -0.16f), new Vector3(0.1f, 0.1f, 0.1f), "Bronze", new Vector3(70, 0, 0));
            Prim(PrimitiveType.Cylinder, vm, "Sleeve", new Vector3(0.05f, -0.14f, -0.38f), new Vector3(0.12f, 0.2f, 0.12f), "Linen", new Vector3(70, 0, 0));
            var swordT = Node("SwordHold", vm, Vector3.zero, new Vector3(70, 0, 0)).transform;
            var blade = PropPrefabs.BuildSword(swordT);
            foreach (var r in vm.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

            // Hand torch for dark levels (enabled by the scene builders).
            var torch = Node("HandTorch", camGo.transform, new Vector3(-0.42f, -0.38f, 0.6f), new Vector3(15, 0, 10)).transform;
            Cyl(torch, "Stick", new Vector3(0, -0.1f, 0), 0.025f, 0.4f, "Wood", false, null, 8);
            var flame = Prim(PrimitiveType.Sphere, torch, "Flame", new Vector3(0, 0.15f, 0), new Vector3(0.08f, 0.16f, 0.08f), "Flame");
            PropPrefabs.Fire(torch, new Vector3(0, 0.1f, 0), 0.4f);
            var l = PointLight(torch, "Light", new Vector3(0, 0.25f, 0.1f), new Color32(0xFF, 0xB7, 0x66, 255), 2.6f, 11f, true);
            var fl = torch.gameObject.AddComponent<TorchFlicker>(); fl.torchLight = l; fl.flame = flame.transform; fl.baseIntensity = 2.6f; fl.amount = 0.2f;
            foreach (var r in torch.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            torch.gameObject.SetActive(false);

            look.cameraRig = rig; look.bobPivot = bob; look.cam = cam;
            inter.eye = camGo.transform;
            sword.eye = camGo.transform; sword.swordPivot = pivot; sword.bladeRenderer = blade.GetComponent<MeshRenderer>();
            SetLayer(root, Layers.Player);
            PropPrefabs.Save(root, "Player");
        }

        // ---------------------------------------------------------------- Spider
        static void BuildSpider()
        {
            var root = new GameObject("Spider");
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.75f; cc.height = 1.6f; cc.center = new Vector3(0, 0.8f, 0); cc.stepOffset = 0.3f; cc.slopeLimit = 50f;
            var hp = root.AddComponent<Health>(); hp.maxHealth = 50f; hp.iFrameTime = 0.2f;
            var ai = root.AddComponent<SpiderAI>();
            var rig = root.AddComponent<SpiderRig>();
            ai.rig = rig;

            var vis = Node("Visual", root.transform, Vector3.zero).transform;
            var bodyT = Node("Body", vis, new Vector3(0, 0.75f, 0)).transform;
            Prim(PrimitiveType.Sphere, bodyT, "Abdomen", new Vector3(0, 0.15f, -0.85f), new Vector3(1.25f, 0.95f, 1.55f), "SpiderBody");
            Prim(PrimitiveType.Sphere, bodyT, "AbdomenMark", new Vector3(0, 0.55f, -0.85f), new Vector3(0.5f, 0.2f, 0.8f), "SpiderLeg");
            Prim(PrimitiveType.Sphere, bodyT, "Thorax", new Vector3(0, 0, 0.2f), new Vector3(0.85f, 0.55f, 0.95f), "SpiderBody");
            Prim(PrimitiveType.Sphere, bodyT, "Head", new Vector3(0, 0.02f, 0.68f), new Vector3(0.5f, 0.38f, 0.45f), "SpiderBody");
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Capsule, bodyT, "Fang" + s, new Vector3(s * 0.1f, -0.2f, 0.88f), new Vector3(0.07f, 0.14f, 0.07f), "Obsidian", new Vector3(-20, 0, s * 8));
                Prim(PrimitiveType.Capsule, bodyT, "Palp" + s, new Vector3(s * 0.2f, -0.12f, 0.85f), new Vector3(0.06f, 0.18f, 0.06f), "SpiderLeg", new Vector3(60, s * 20, 0));
            }
            // Eight milky, glowing, sightless eyes.
            Vector2[] eyes = { new Vector2(-0.07f, 0.12f), new Vector2(0.07f, 0.12f), new Vector2(-0.16f, 0.08f), new Vector2(0.16f, 0.08f),
                               new Vector2(-0.1f, 0.2f), new Vector2(0.1f, 0.2f), new Vector2(-0.2f, 0.16f), new Vector2(0.2f, 0.16f) };
            for (int i = 0; i < eyes.Length; i++)
                Prim(PrimitiveType.Sphere, bodyT, "Eye" + i, new Vector3(eyes[i].x, eyes[i].y, 0.86f), Vector3.one * (i < 2 ? 0.09f : 0.06f), "SpiderEye");

            var hips = new List<Transform>(); var knees = new List<Transform>();
            float[] angles = { 35, 75, 110, 145 };
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < 4; i++)
                {
                    float sgn = side == 0 ? -1 : 1;
                    var hip = Node($"Hip{side}{i}", bodyT, new Vector3(sgn * 0.32f, 0.02f, 0.38f - i * 0.17f), new Vector3(-38, sgn * angles[i], 0)).transform;
                    Prim(PrimitiveType.Capsule, hip, "Upper", new Vector3(0, 0, 0.5f), new Vector3(0.1f, 0.55f, 0.1f), "SpiderLeg", new Vector3(90, 0, 0));
                    var knee = Node("Knee", hip, new Vector3(0, 0, 1.0f), new Vector3(95, 0, 0)).transform;
                    Prim(PrimitiveType.Sphere, knee, "Joint", Vector3.zero, Vector3.one * 0.13f, "SpiderBody");
                    Prim(PrimitiveType.Capsule, knee, "Lower", new Vector3(0, 0, 0.62f), new Vector3(0.075f, 0.65f, 0.075f), "SpiderLeg", new Vector3(90, 0, 0));
                    hips.Add(hip); knees.Add(knee);
                }
            rig.body = bodyT; rig.hips = hips.ToArray(); rig.knees = knees.ToArray();

            // Minimap dot (MinimapIcon layer: only the map camera sees it).
            var icon = Box(root.transform, "MapIcon", new Vector3(0, 5.2f, 0), new Vector3(1.6f, 0.05f, 1.6f), "MapSpider", false);
            icon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            icon.AddComponent<MinimapMarker>().target = root.transform;
            SetLayer(root, Layers.Enemy);
            icon.layer = Layers.Minimap;
            PropPrefabs.Save(root, "Spider");
        }

        // ---------------------------------------------------------------- Scorpion
        static void BuildScorpion()
        {
            var root = new GameObject("Scorpion");
            var sc = root.AddComponent<Scorpion>();
            var vis = Node("Visual", root.transform, Vector3.zero).transform;
            vis.localScale = Vector3.one * 1.35f;
            var body = Node("Body", vis, new Vector3(0, 0.3f, 0)).transform;
            Box(body, "Carapace", new Vector3(0, 0, 0.32f), new Vector3(0.55f, 0.2f, 0.6f), "ScorpionShell", false, 0.5f);
            for (int i = 0; i < 4; i++)
                Box(body, "Seg" + i, new Vector3(0, 0.02f, -0.05f - i * 0.2f), new Vector3(0.5f - i * 0.04f, 0.18f, 0.22f), "ScorpionShell", false, 0.5f);
            Prim(PrimitiveType.Sphere, body, "EyeL", new Vector3(-0.08f, 0.12f, 0.55f), Vector3.one * 0.05f, "SpiderEye");
            Prim(PrimitiveType.Sphere, body, "EyeR", new Vector3(0.08f, 0.12f, 0.55f), Vector3.one * 0.05f, "SpiderEye");

            var legs = new List<Transform>();
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                {
                    var hip = Node($"Leg{s}{i}", body, new Vector3(s * 0.25f, -0.03f, 0.4f - i * 0.17f), new Vector3(-20, s * (70 + i * 15), 0)).transform;
                    Prim(PrimitiveType.Capsule, hip, "Upper", new Vector3(0, 0, 0.2f), new Vector3(0.05f, 0.22f, 0.05f), "ScorpionShell", new Vector3(90, 0, 0));
                    Prim(PrimitiveType.Capsule, hip, "Lower", new Vector3(0, -0.15f, 0.45f), new Vector3(0.04f, 0.2f, 0.04f), "ScorpionShell", new Vector3(140, 0, 0));
                    legs.Add(hip);
                }
            Transform clawL = null, clawR = null;
            for (int s = -1; s <= 1; s += 2)
            {
                var arm = Node("Arm" + s, body, new Vector3(s * 0.22f, 0, 0.6f), new Vector3(0, s * 25, 0)).transform;
                Prim(PrimitiveType.Capsule, arm, "Upper", new Vector3(0, 0, 0.2f), new Vector3(0.08f, 0.22f, 0.08f), "ScorpionShell", new Vector3(90, 0, 0));
                var claw = Node("Claw", arm, new Vector3(0, 0, 0.45f)).transform;
                Box(claw, "Palm", new Vector3(0, 0, 0.1f), new Vector3(0.18f, 0.12f, 0.25f), "ScorpionShell", false, 0.3f);
                Box(claw, "Finger", new Vector3(s * 0.05f, 0, 0.3f), new Vector3(0.06f, 0.07f, 0.22f), "Obsidian", false, 0.3f);
                if (s < 0) clawL = claw; else clawR = claw;
            }
            var tail = new List<Transform>();
            Transform parent = body; Vector3 pos = new Vector3(0, 0.05f, -0.8f); Vector3 rot = new Vector3(-35, 180, 0);
            for (int i = 0; i < 6; i++)
            {
                var seg = Node("Tail" + i, parent, pos, rot).transform;
                float w = 0.17f - i * 0.015f;
                Box(seg, "Seg", new Vector3(0, 0, 0.13f), new Vector3(w, w, 0.26f), "ScorpionShell", false, 0.3f);
                tail.Add(seg);
                parent = seg; pos = new Vector3(0, 0, 0.25f); rot = new Vector3(-30, 0, 0);
            }
            var bulb = Prim(PrimitiveType.Sphere, parent, "Bulb", new Vector3(0, 0, 0.3f), new Vector3(0.14f, 0.12f, 0.16f), "ScorpionShell");
            Cyl(parent, "Stinger", new Vector3(0, -0.06f, 0.42f), 0.035f, 0.2f, "Venom", false, new Vector3(120, 0, 0), 8, 0.0f);
            sc.body = body; sc.legs = legs.ToArray(); sc.tailSegments = tail.ToArray(); sc.clawL = clawL; sc.clawR = clawR;
            PropPrefabs.Save(root, "Scorpion");
        }

        // ---------------------------------------------------------------- Stone Master
        static void BuildStoneMaster()
        {
            var root = new GameObject("StoneMaster");
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 1.1f; cc.height = 4.7f; cc.center = new Vector3(0, 2.35f, 0); cc.stepOffset = 0.5f; cc.slopeLimit = 30f;
            var hp = root.AddComponent<Health>(); hp.maxHealth = 300f; hp.iFrameTime = 0.15f;
            var ai = root.AddComponent<StoneMasterAI>();
            var rig = root.AddComponent<StoneMasterRig>();
            var runes = new List<Renderer>();

            var vis = Node("Visual", root.transform, Vector3.zero).transform;
            var hips = Node("Hips", vis, new Vector3(0, 2.2f, 0)).transform;
            Box(hips, "Pelvis", new Vector3(0, 0.1f, 0), new Vector3(1.5f, 0.55f, 0.95f), "GolemStone", false, 1f);
            Box(hips, "Loincloth", new Vector3(0, -0.35f, 0.45f), new Vector3(0.8f, 0.7f, 0.1f), "MossRock", false, 1f, new Vector3(6, 0, 0));

            var spine = Node("Spine", hips, new Vector3(0, 0.3f, 0)).transform;
            Box(spine, "Abdomen", new Vector3(0, 0.25f, 0), new Vector3(1.4f, 0.6f, 0.95f), "GolemStone", false, 1f);
            Box(spine, "Chest", new Vector3(0, 0.95f, -0.05f), new Vector3(2.1f, 1.0f, 1.25f), "GolemStone", false, 1f);
            Box(spine, "Back", new Vector3(0, 1.0f, -0.55f), new Vector3(1.6f, 0.9f, 0.4f), "MossRock", false, 1f);
            Box(spine, "MossDrape", new Vector3(0, 1.48f, 0.05f), new Vector3(2.0f, 0.18f, 1.2f), "MossRock", false, 1f);
            // Weak point: core behind sliding plates.
            var chest = Node("CoreHousing", spine, new Vector3(0, 0.95f, 0.5f)).transform;
            var coreGo = Prim(PrimitiveType.Sphere, chest, "Core", new Vector3(0, 0, 0.0f), Vector3.one * 0.5f, "GolemCore");
            var coreCol = coreGo.AddComponent<SphereCollider>(); coreCol.radius = 0.9f;
            coreGo.tag = Layers.TagWeakPoint;
            var wp = coreGo.AddComponent<WeakPoint>(); wp.owner = hp;
            var plateL = Box(chest, "PlateL", new Vector3(-0.27f, 0, 0.2f), new Vector3(0.55f, 0.75f, 0.16f), "GolemStone", false, 0.6f).transform;
            var plateR = Box(chest, "PlateR", new Vector3(0.27f, 0, 0.2f), new Vector3(0.55f, 0.75f, 0.16f), "GolemStone", false, 0.6f).transform;
            runes.Add(Box(plateL, "Rune", new Vector3(0, 0, 0.085f), new Vector3(0.06f, 0.55f, 0.01f), "GolemRune", false, 0.3f).GetComponent<Renderer>());
            runes.Add(Box(plateR, "Rune", new Vector3(0, 0, 0.085f), new Vector3(0.06f, 0.55f, 0.01f), "GolemRune", false, 0.3f).GetComponent<Renderer>());
            var coreLight = PointLight(chest, "CoreLight", new Vector3(0, 0, 0.5f), new Color(0.25f, 0.82f, 1f), 0.4f, 7f);
            var core = chest.gameObject.AddComponent<StoneMasterCore>();
            core.plateL = plateL; core.plateR = plateR; core.coreRenderer = coreGo.GetComponent<Renderer>(); core.coreLight = coreLight; core.coreCollider = coreCol;
            for (int i = 0; i < 3; i++)
                runes.Add(Box(spine, "ChestRune" + i, new Vector3(-0.75f + i * 0.75f, 0.42f, 0.485f), new Vector3(0.38f, 0.05f, 0.01f), "GolemRune", false, 0.3f).GetComponent<Renderer>());

            // Violet crystal shards on shoulders and back.
            var rnd = new System.Random(3);
            for (int i = 0; i < 9; i++)
            {
                float side = i < 4 ? -1 : (i < 8 ? 1 : 0);
                Vector3 p = side == 0 ? new Vector3(0, 1.4f, -0.7f) : new Vector3(side * (0.75f + (float)rnd.NextDouble() * 0.3f), 1.5f, -0.3f + (float)rnd.NextDouble() * 0.4f);
                Box(spine, "Crystal" + i, p, new Vector3(0.18f, 0.7f + (float)rnd.NextDouble() * 0.5f, 0.18f), "GolemCrystal", false, 0.3f,
                    new Vector3((float)rnd.NextDouble() * 30 - 15, (float)rnd.NextDouble() * 90, side * -25 + (float)rnd.NextDouble() * 20 - 10));
            }

            // Head: heavy brow, burning eyes behind stone lids, horned crown, jaw with teeth.
            var head = Node("Head", spine, new Vector3(0, 1.55f, 0.05f)).transform;
            Box(head, "Neck", new Vector3(0, 0.05f, 0), new Vector3(0.6f, 0.3f, 0.55f), "GolemStone", false, 0.6f);
            Box(head, "Skull", new Vector3(0, 0.45f, 0.05f), new Vector3(0.9f, 0.75f, 0.85f), "GolemStone", false, 0.8f);
            Box(head, "Brow", new Vector3(0, 0.62f, 0.48f), new Vector3(0.95f, 0.16f, 0.2f), "GolemStone", false, 0.5f, new Vector3(12, 0, 0));
            Box(head, "Jaw", new Vector3(0, 0.12f, 0.3f), new Vector3(0.75f, 0.25f, 0.45f), "GolemStone", false, 0.5f);
            for (int i = 0; i < 5; i++) Box(head, "Tooth" + i, new Vector3(-0.24f + i * 0.12f, 0.27f, 0.53f), new Vector3(0.06f, 0.11f, 0.05f), "Sandstone", false, 0.2f);
            var eyeR = new List<Renderer>(); var lids = new List<Transform>();
            for (int s = -1; s <= 1; s += 2)
            {
                var anchor = Node("EyeAnchor" + s, head, new Vector3(s * 0.2f, 0.47f, 0.47f)).transform;
                eyeR.Add(Prim(PrimitiveType.Sphere, anchor, "Eye", Vector3.zero, new Vector3(0.17f, 0.11f, 0.08f), "GolemEye").GetComponent<Renderer>());
                lids.Add(Box(anchor, "Lid", new Vector3(0, 0, 0.04f), new Vector3(0.22f, 0.15f, 0.05f), "GolemStone", false, 0.3f).transform);
            }
            var eyeLight = PointLight(head, "EyeLight", new Vector3(0, 0.5f, 0.9f), new Color(1f, 0.4f, 0.12f), 0f, 6f);
            Cyl(head, "Crown", new Vector3(0, 1.08f, 0.02f), 0.4f, 0.6f, "GolemStone", false, null, 16, 0.46f, 1f);
            for (int i = 0; i < 3; i++) runes.Add(Cyl(head, "CrownRune" + i, new Vector3(0, 0.88f + i * 0.17f, 0.02f), 0.415f + i * 0.012f, 0.04f, "GolemRune", false, null, 16).GetComponent<Renderer>());
            for (int s = -1; s <= 1; s += 2)
            {   // Great curved horns.
                Transform h = Node("Horn" + s, head, new Vector3(s * 0.45f, 0.7f, 0.05f), new Vector3(0, 0, s * -70)).transform;
                for (int k = 0; k < 4; k++)
                {
                    Box(h, "H" + k, new Vector3(0, 0.15f, 0), new Vector3(0.2f - k * 0.04f, 0.32f, 0.2f - k * 0.04f), "Obsidian", false, 0.3f);
                    h = Node("Next", h, new Vector3(0, 0.28f, 0), new Vector3(-12, 0, s * 25)).transform;
                }
            }

            // Arms.
            Transform Arm(int s, out Transform elbow, out Transform hand)
            {
                var sh = Node(s < 0 ? "ShoulderL" : "ShoulderR", spine, new Vector3(s * 1.3f, 1.25f, -0.05f)).transform;
                Box(sh, "Pauldron", new Vector3(s * 0.08f, 0.12f, 0), new Vector3(0.85f, 0.55f, 0.85f), "MossRock", false, 0.7f);
                Box(sh, "UpperArm", new Vector3(0, -0.55f, 0), new Vector3(0.58f, 1.1f, 0.58f), "GolemStone", false, 0.8f);
                runes.Add(Box(sh, "ArmRune", new Vector3(s * 0.295f, -0.55f, 0), new Vector3(0.01f, 0.8f, 0.07f), "GolemRune", false, 0.3f).GetComponent<Renderer>());
                elbow = Node(s < 0 ? "ElbowL" : "ElbowR", sh, new Vector3(0, -1.1f, 0)).transform;
                Box(elbow, "Forearm", new Vector3(0, -0.5f, 0), new Vector3(0.62f, 1.0f, 0.62f), "GolemStone", false, 0.8f);
                runes.Add(Box(elbow, "Band", new Vector3(0, -0.2f, 0), new Vector3(0.66f, 0.06f, 0.66f), "GolemRune", false, 0.3f).GetComponent<Renderer>());
                hand = Node("Hand", elbow, new Vector3(0, -1.15f, 0)).transform;
                Box(hand, "Fist", Vector3.zero, new Vector3(0.72f, 0.6f, 0.72f), "GolemStone", false, 0.6f);
                return sh;
            }
            var shL = Arm(-1, out var elL, out _);
            var shR = Arm(1, out var elR, out var handR);

            // Weapon: great spear of obsidian bound with gold.
            var spear = Node("Spear", handR, Vector3.zero).transform;
            Cyl(spear, "Shaft", new Vector3(0, 0, 0.5f), 0.085f, 5.4f, "Wood", false, new Vector3(90, 0, 0), 10);
            Box(spear, "Blade", new Vector3(0, 0, 3.55f), new Vector3(0.34f, 0.07f, 0.95f), "Obsidian", false, 0.5f);
            Box(spear, "Tip", new Vector3(0, 0, 4.05f), new Vector3(0.24f, 0.07f, 0.24f), "Obsidian", false, 0.3f, new Vector3(0, 45, 0));
            Cyl(spear, "Bind1", new Vector3(0, 0, 3.0f), 0.11f, 0.1f, "Gold", false, new Vector3(90, 0, 0), 10);
            Cyl(spear, "Bind2", new Vector3(0, 0, 2.85f), 0.11f, 0.06f, "Gold", false, new Vector3(90, 0, 0), 10);
            Box(spear, "Pommel", new Vector3(0, 0, -2.25f), new Vector3(0.35f, 0.35f, 0.45f), "GolemStone", false, 0.4f);

            // Legs.
            Transform Leg(int s, out Transform knee)
            {
                var leg = Node(s < 0 ? "LegL" : "LegR", hips, new Vector3(s * 0.5f, -0.05f, 0)).transform;
                Box(leg, "Thigh", new Vector3(0, -0.55f, 0), new Vector3(0.7f, 1.1f, 0.72f), "GolemStone", false, 0.8f);
                knee = Node(s < 0 ? "KneeL" : "KneeR", leg, new Vector3(0, -1.1f, 0)).transform;
                Box(knee, "KneeCap", new Vector3(0, 0, 0.3f), new Vector3(0.5f, 0.4f, 0.25f), "MossRock", false, 0.4f);
                Box(knee, "Shin", new Vector3(0, -0.55f, 0), new Vector3(0.64f, 1.1f, 0.64f), "GolemStone", false, 0.8f);
                Box(knee, "Foot", new Vector3(0, -1.0f, 0.18f), new Vector3(0.8f, 0.25f, 1.15f), "GolemStone", false, 0.6f);
                return leg;
            }
            var legL = Leg(-1, out var kneeL);
            var legR = Leg(1, out var kneeR);

            rig.hips = hips; rig.spine = spine; rig.head = head;
            rig.shoulderL = shL; rig.elbowL = elL; rig.shoulderR = shR; rig.elbowR = elR;
            rig.legL = legL; rig.kneeL = kneeL; rig.legR = legR; rig.kneeR = kneeR; rig.shinLength = 1.1f;
            ai.rig = rig; ai.core = core; ai.eyes = eyeR.ToArray(); ai.runes = runes.ToArray(); ai.eyelids = lids.ToArray();
            ai.eyeLight = eyeLight; ai.handR = handR; ai.visualRoot = vis;
            SetLayer(root, Layers.Enemy);
            PropPrefabs.Save(root, "StoneMaster");
        }
    }
}
