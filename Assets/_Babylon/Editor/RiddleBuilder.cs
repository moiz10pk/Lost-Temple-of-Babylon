using UnityEngine;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>03_Riddles: pillared lapis hall, winged bull on a stepped dais, rune wheel, braziers, lectern, sword pedestal.</summary>
    public static class RiddleBuilder
    {
        const float HW = 12f, L = 34f, H = 10f;

        public static void Build()
        {
            var s = SceneCommon.New("03_Riddles");
            SceneCommon.Lighting(null, new Color(0.14f, 0.12f, 0.13f), new Color(0.1f, 0.08f, 0.07f), new Color(0.04f, 0.03f, 0.03f),
                new Color(0.09f, 0.07f, 0.06f), 0.022f);
            SceneCommon.Volume("Vol_Riddles");
            SceneCommon.Info(3, "Round 2 - The Riddles", "Solve ancient riddles", MusicId.Mysterious, SoundId.CaveDrips,
                "Answer the three riddles at the stone lectern",
                "Three riddles guard the Sword of Marduk. Answer all three truly - a single wrong answer and the scorpion strikes, and the trial begins again.", false, 0.5f);

            var hall = new GameObject("RiddleHall").transform;
            Shell(hall);
            Columns(hall);
            Dais(hall);
            var wheel = RuneWheel(hall);
            var braziers = new Brazier[3];
            float[] bx = { -4f, 0f, 4f };
            for (int i = 0; i < 3; i++)
            {
                var b = Place("Brazier", hall, new Vector3(bx[i], 0, i == 1 ? 25.4f : 24.9f));
                braziers[i] = b.GetComponent<Brazier>(); braziers[i].litOnStart = false;
            }
            Place("Brazier", hall, new Vector3(-4.5f, 0, 3.5f));
            Place("Brazier", hall, new Vector3(4.5f, 0, 3.5f));

            // Exit door on the east wall and the corridor to the Guardian.
            var door = Box(hall, "ExitDoor", new Vector3(HW - 0.25f, 2.3f, 30f), new Vector3(0.6f, 4.6f, 3.2f), "CarvedWall", true, 1.5f);
            var sd = door.AddComponent<StoneDoor>(); sd.openOffset = new Vector3(0, -4.55f, 0); sd.duration = 3.5f;
            Box(hall, "DoorFrame", new Vector3(HW - 0.35f, 4.9f, 30f), new Vector3(0.7f, 0.6f, 4.2f), "Gold", false, 1f);
            Corridor(hall);

            // Lectern with the riddle tablet.
            var lectern = new GameObject("RiddleLectern"); lectern.transform.SetParent(hall, false);
            lectern.transform.localPosition = new Vector3(0, 0, 17f);
            lectern.layer = Layers.Interactable;
            var lv = Node("Visual", lectern.transform, Vector3.zero).transform;
            Box(lv, "Base", new Vector3(0, 0.15f, 0), new Vector3(1.4f, 0.3f, 1.0f), "SandstoneDark", false, 1f);
            Box(lv, "Column", new Vector3(0, 0.75f, 0), new Vector3(0.7f, 1.0f, 0.5f), "CarvedWall", false, 1f);
            Box(lv, "Top", new Vector3(0, 1.32f, 0.05f), new Vector3(1.1f, 0.12f, 0.8f), "Gold", false, 1f, new Vector3(-20, 0, 0));
            Box(lv, "Tablet", new Vector3(0, 1.42f, 0.05f), new Vector3(0.8f, 0.06f, 0.55f), "ClayGlow", false, 0.6f, new Vector3(-20, 0, 0));
            PointLight(lectern.transform, "Glow", new Vector3(0, 1.9f, -0.6f), new Color(1f, 0.8f, 0.45f), 1.2f, 4f);
            var lc = lectern.AddComponent<BoxCollider>(); lc.size = new Vector3(1.4f, 1.6f, 1.0f); lc.center = new Vector3(0, 0.8f, 0);
            var tab = lectern.AddComponent<RiddleTablet>();
            Box(hall, "FloorCrack", new Vector3(0, 0.005f, 15.2f), new Vector3(1.2f, 0.01f, 0.4f), "Obsidian", false, 0.5f, new Vector3(0, 15, 0));

            // Sword pedestal (rises when solved).
            var ped = new GameObject("SwordPedestal"); ped.transform.SetParent(hall, false);
            ped.transform.localPosition = new Vector3(0, 0, 22f);
            ped.layer = Layers.Interactable;
            var pv = Node("Visual", ped.transform, Vector3.zero).transform;
            Box(pv, "Plinth", new Vector3(0, 0.6f, 0), new Vector3(0.9f, 1.2f, 0.9f), "CarvedWall", false, 1f);
            Box(pv, "Cap", new Vector3(0, 1.25f, 0), new Vector3(1.05f, 0.1f, 1.05f), "Gold", false, 1f);
            var swordVis = Node("FloatingSword", pv, new Vector3(0, 1.55f, 0)).transform;
            PropPrefabs.BuildSword(swordVis);
            var mo = swordVis.gameObject.AddComponent<SimpleMotion>(); mo.spin = new Vector3(0, 45, 0); mo.bobHeight = 0.08f;
            var glow = PointLight(ped.transform, "Glow", new Vector3(0, 2.2f, 0), new Color(1f, 0.75f, 0.35f), 3f, 6f);
            var pc = ped.AddComponent<BoxCollider>(); pc.size = new Vector3(1.1f, 2.6f, 1.1f); pc.center = new Vector3(0, 1.3f, 0);
            var pedestal = ped.AddComponent<SwordPedestal>(); pedestal.swordVisual = swordVis.gameObject; pedestal.glow = glow; pedestal.verb = "take the Sword of Marduk"; pedestal.riseHeight = 2.7f;

            var master = new GameObject("RiddleMaster").AddComponent<RiddleMaster>();
            master.wheel = wheel; master.braziers = braziers; master.pedestal = pedestal; master.exitDoor = sd;
            tab.master = master; pedestal.master = master;

            // A blind spider prowls the western aisle.
            var area = new GameObject("WestAisle").AddComponent<OpenArea>();
            area.patrolCenter = new Vector3(-9.3f, 0, 15f); area.patrolSize = new Vector3(3f, 0, 20f);
            area.roamCenter = new Vector3(0, 0, 16f); area.roamSize = new Vector3(HW * 2 - 2, 0, L - 4);
            var sp = Place("Spider", hall, new Vector3(-9.3f, 0.05f, 10f), 0, 0.85f);
            sp.GetComponent<SpiderAI>().area = area;

            SceneCommon.ReflectionProbe(new Vector3(0, 4, 17), new Vector3(26, 12, 36));
            SceneCommon.Player(new Vector3(0, 0.05f, 1.8f), 0, false);
            SceneCommon.UI(riddle: true);
            SceneCommon.Save(s, "03_Riddles");
        }

        static void Shell(Transform hall)
        {
            Box(hall, "Floor", new Vector3(0, -0.15f, L / 2), new Vector3(HW * 2, 0.3f, L), "CrackedFloor", true, 3f);
            Box(hall, "Ceiling", new Vector3(0, H + 0.25f, L / 2), new Vector3(HW * 2, 0.5f, L), "CaveRock", true, 4f).layer = Layers.Ceiling;
            Box(hall, "WallN", new Vector3(0, H / 2, L + 0.25f), new Vector3(HW * 2 + 1, H, 0.5f), "CarvedWall", true, 2.5f);
            Box(hall, "WallS", new Vector3(0, H / 2, -0.25f), new Vector3(HW * 2 + 1, H, 0.5f), "CarvedWall", true, 2.5f);
            Box(hall, "WallW", new Vector3(-HW - 0.25f, H / 2, L / 2), new Vector3(0.5f, H, L), "CarvedWall", true, 2.5f);
            // East wall split around the door (z 28.4..31.6).
            Box(hall, "WallE1", new Vector3(HW + 0.25f, H / 2, 14.2f), new Vector3(0.5f, H, 28.4f), "CarvedWall", true, 2.5f);
            Box(hall, "WallE2", new Vector3(HW + 0.25f, H / 2, 32.8f), new Vector3(0.5f, H, 2.4f), "CarvedWall", true, 2.5f);
            Box(hall, "WallE3", new Vector3(HW + 0.25f, 4.6f + (H - 4.6f) / 2, 30f), new Vector3(0.5f, H - 4.6f, 3.2f), "CarvedWall", true, 2.5f);
            // Lapis wainscot and gold trim.
            foreach (var (pos, size) in new[] {
                (new Vector3(-HW + 0.03f, 1.3f, L / 2), new Vector3(0.06f, 2.6f, L)),
                (new Vector3(HW - 0.03f, 1.3f, 14.2f), new Vector3(0.06f, 2.6f, 28.4f)),
                (new Vector3(0, 1.3f, L - 0.03f), new Vector3(HW * 2, 2.6f, 0.06f)),
                (new Vector3(0, 1.3f, 0.03f), new Vector3(HW * 2, 2.6f, 0.06f)) })
            {
                Box(hall, "Wainscot", pos, size, "LapisTile", false, 1.3f);
                Box(hall, "Trim", pos + Vector3.up * 1.35f, new Vector3(Mathf.Max(size.x, 0.1f), 0.12f, Mathf.Max(size.z, 0.1f)), "Gold", false, 1f);
            }
            for (int i = 0; i < 6; i++)
                Box(hall, "Beam" + i, new Vector3(0, H - 0.3f, 3 + i * 6f), new Vector3(HW * 2, 0.5f, 0.6f), "Gold", false, 1f);
            for (int i = 0; i < 5; i++)
            {
                Place("WallTorch", hall, new Vector3(-HW + 0.02f, 3.4f, 4 + i * 6.5f), 90);
                if (i != 4) Place("WallTorch", hall, new Vector3(HW - 0.02f, 3.4f, 4 + i * 6.5f), -90);
            }
        }

        static void Columns(Transform hall)
        {
            for (int i = 0; i < 4; i++)
                for (int sgn = -1; sgn <= 1; sgn += 2)
                    PropPrefabs.BuildPillar(hall, $"Column{i}{sgn}", new Vector3(sgn * 6.5f, 0, 7 + i * 6f), 7.4f);
        }

        static void Dais(Transform hall)
        {
            Box(hall, "Dais1", new Vector3(0, 0.2f, 30.3f), new Vector3(11, 0.4f, 8), "SandstoneDark", true, 2f);
            Box(hall, "Dais2", new Vector3(0, 0.6f, 30.8f), new Vector3(8.5f, 0.4f, 6.5f), "CarvedWall", true, 2f);
            Box(hall, "Dais3", new Vector3(0, 1.0f, 31f), new Vector3(6, 0.4f, 5.4f), "LapisTile", true, 1.5f);
            Box(hall, "DaisTrim", new Vector3(0, 1.22f, 31f), new Vector3(6.2f, 0.06f, 5.6f), "Gold", false, 1f);
            Place("Lamassu", hall, new Vector3(0, 1.2f, 30.9f), 180, 1.35f);
        }

        static RuneWheel RuneWheel(Transform hall)
        {
            var root = Node("RuneWheel", hall, new Vector3(0, 7.4f, L - 0.12f), new Vector3(-90, 0, 0));
            var spin = Node("Spinner", root.transform, Vector3.zero).transform;
            var backing = Mesh(root.transform, "Backing", MeshGen.Fan(360), "GolemStone", new Vector3(0, -0.02f, 0), null, Vector3.one * 2.2f);
            Mesh(root.transform, "Rim", MeshGen.Ring(0.92f), "Gold", new Vector3(0, -0.01f, 0), null, Vector3.one * 2.32f);
            var segs = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var seg = Mesh(spin, "Segment" + i, MeshGen.Ring(0.62f, 24, i * 120f + 6f, i * 120f + 114f), "RuneOff", Vector3.zero, null, Vector3.one * 1.95f);
                segs[i] = seg.GetComponent<Renderer>();
            }
            Mesh(spin, "Glyph", MeshGen.Fan(360), "CuneiformFaint", new Vector3(0, 0.01f, 0), null, Vector3.one * 1.0f);
            var w = spin.gameObject.AddComponent<RuneWheel>();
            w.segments = segs; w.offMaterial = M("RuneOff"); w.onMaterial = M("RuneOn");
            w.wheelLight = PointLight(root.transform, "WheelLight", new Vector3(0, 1.5f, 0), new Color(0.25f, 0.82f, 1f), 0.5f, 10f);
            return w;
        }

        static void Corridor(Transform hall)
        {
            var c = new GameObject("Corridor").transform; c.SetParent(hall, false);
            Box(c, "Floor", new Vector3(HW + 5, -0.15f, 30f), new Vector3(10, 0.3f, 3.6f), "CrackedFloor", true, 3f);
            Box(c, "Ceil", new Vector3(HW + 5, 4.85f, 30f), new Vector3(10, 0.3f, 3.6f), "CaveRock", true, 3f).layer = Layers.Ceiling;
            Box(c, "N", new Vector3(HW + 5, 2.4f, 31.85f), new Vector3(10, 5, 0.3f), "CaveRock", true, 3f);
            Box(c, "S", new Vector3(HW + 5, 2.4f, 28.15f), new Vector3(10, 5, 0.3f), "CaveRock", true, 3f);
            Box(c, "E", new Vector3(HW + 10.1f, 2.4f, 30f), new Vector3(0.3f, 5, 3.6f), "CaveRock", true, 3f);
            PointLight(c, "Gloom", new Vector3(HW + 7, 3.5f, 30f), new Color(0.3f, 0.5f, 1f), 1.5f, 8f);
            Trigger(c, "ToBoss", new Vector3(HW + 8.5f, 1.5f, 30f), new Vector3(2, 3, 3.4f)).gameObject.AddComponent<SceneExitTrigger>().sceneName = "04_Boss";
        }
    }
}
