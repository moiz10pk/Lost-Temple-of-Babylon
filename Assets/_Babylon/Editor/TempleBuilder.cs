using UnityEngine;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>01_Temple: golden-hour desert, Ziggurat of Ur-style temple, the shrine and the hidden entrance puzzle.</summary>
    public static class TempleBuilder
    {
        // Tier half-sizes and heights (bottom, top).
        static readonly float[] Half = { 22f, 16f, 10f };
        static readonly float[] Bottom = { 0f, 8f, 15f };
        static readonly float[] Top = { 8f, 15f, 21f };
        public const float Summit = 21f;
        // Hidden shaft in the summit (x range, z range).
        const float HX0 = -4.25f, HX1 = -1.75f, HZ0 = -1.5f, HZ1 = 5.5f;

        public static void Build()
        {
            var s = SceneCommon.New("01_Temple");
            SceneCommon.Lighting("Sky_Sunset", new Color(0.58f, 0.5f, 0.55f), new Color(0.62f, 0.46f, 0.33f), new Color(0.25f, 0.18f, 0.12f),
                new Color(0.86f, 0.63f, 0.45f), 0.0035f, FogMode.Exponential);
            SceneCommon.Sun(new Vector3(9, 62, 0), new Color(1f, 0.72f, 0.45f), 1.7f);
            SceneCommon.Volume("Vol_Temple");
            var info = SceneCommon.Info(1, "The Temple", "Explore the ancient temple", MusicId.Calm, SoundId.DesertWind, "Explore the temple",
                "The great ziggurat rises from the sands, its terraces burning gold in the dying sun. Climb to the shrine at its summit.", false, 0.55f);
            info.updateEnvironmentLighting = true;

            var world = new GameObject("World").transform;
            Desert(world);
            BuildZiggurat(world, true);
            Shrine(world);
            Approach(world);
            Bounds(world);
            SceneCommon.ReflectionProbe(new Vector3(0, 24, 0), new Vector3(60, 30, 60));

            SceneCommon.Player(new Vector3(0, 0.1f, -108f), 0, false);
            SceneCommon.UI();
            SceneCommon.Save(s, "01_Temple");
        }

        static void Desert(Transform world)
        {
            var dunes = MeshGen.Dunes(520f, 170, 10f, new[] { new Vector4(0, 0, 32, 32), new Vector4(0, -75, 10, 45) });
            var go = Mesh(world, "Dunes", dunes, "Sand", Vector3.zero);
            go.AddComponent<MeshCollider>().sharedMesh = dunes;
            Box(world, "Plaza", new Vector3(0, -0.05f, -30f), new Vector3(70, 0.1f, 20), "CrackedFloor", true, 3f);
            Box(world, "Road", new Vector3(0, -0.04f, -75f), new Vector3(7, 0.1f, 72), "CrackedFloor", true, 3f);

            var rnd = new System.Random(1750);
            Vector2[] clusters = { new Vector2(-24, -62), new Vector2(-30, -82), new Vector2(22, -58), new Vector2(28, -84), new Vector2(-42, -22),
                                   new Vector2(42, -18), new Vector2(38, 34), new Vector2(-36, 36), new Vector2(-14, -100), new Vector2(16, -104) };
            foreach (var c in clusters)
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3(c.x + (float)rnd.NextDouble() * 6 - 3, -0.1f, c.y + (float)rnd.NextDouble() * 6 - 3);
                    Place("DatePalm", world, p, (float)rnd.NextDouble() * 360, 0.85f + (float)rnd.NextDouble() * 0.4f);
                }
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2, r = 30 + (float)rnd.NextDouble() * 50;
                var p = new Vector3(Mathf.Cos(a) * r, -0.2f, Mathf.Sin(a) * r - 30);
                if (Mathf.Abs(p.x) < 8 && p.z < -30) continue;
                Place("Rock" + (1 + i % 3), world, p, (float)rnd.NextDouble() * 360, 0.6f + (float)rnd.NextDouble() * 1.6f);
            }
        }

        /// <summary>Three-tier ziggurat with converging staircases, tile bands, lion reliefs, pilasters and torches.</summary>
        public static void BuildZiggurat(Transform world, bool withShaft)
        {
            var z = new GameObject("Ziggurat").transform; z.SetParent(world, false);
            var deco = new GameObject("Decor").transform; deco.SetParent(z, false);
            for (int t = 0; t < 3; t++)
            {
                float h = Top[t] - Bottom[t], hs = Half[t];
                if (t == 2 && withShaft) BoxWithHole(z, "Tier3", Bottom[t], Top[t], hs, "MudBrick");
                else Box(z, "Tier" + (t + 1), new Vector3(0, Bottom[t] + h / 2, 0), new Vector3(hs * 2, h, hs * 2), "MudBrick", true, 2.5f);
                // Blue glazed band with gold trims and a parapet on every face.
                for (int f = 0; f < 4; f++)
                {
                    var rot = Quaternion.Euler(0, f * 90, 0);
                    Vector3 outward = rot * Vector3.back;
                    Vector3 along = rot * Vector3.right;
                    float bandY = Bottom[t] + h * 0.62f;
                    var band = Box(deco, $"Band{t}{f}", outward * (hs + 0.06f) + Vector3.up * bandY, new Vector3(hs * 2 - 0.2f, 1.3f, 0.12f), "LapisTile", false, 1.3f, new Vector3(0, f * 90, 0));
                    Box(deco, $"TrimA{t}{f}", outward * (hs + 0.1f) + Vector3.up * (bandY + 0.7f), new Vector3(hs * 2, 0.12f, 0.12f), "Gold", false, 1f, new Vector3(0, f * 90, 0));
                    Box(deco, $"TrimB{t}{f}", outward * (hs + 0.1f) + Vector3.up * (bandY - 0.7f), new Vector3(hs * 2, 0.12f, 0.12f), "Gold", false, 1f, new Vector3(0, f * 90, 0));
                    if (f == 0)
                    {   // South parapet has a gap where the stairs arrive.
                        float gap = t == 0 ? 10f : 4f, len = hs - gap;
                        for (int sgn = -1; sgn <= 1; sgn += 2)
                            Box(z, $"Parapet{t}{f}{sgn}", outward * (hs - 0.3f) + along * sgn * (gap + len / 2) + Vector3.up * (Top[t] + 0.35f), new Vector3(len, 0.7f, 0.6f), "MudBrick", true, 1.5f);
                    }
                    else Box(z, $"Parapet{t}{f}", outward * (hs - 0.3f) + Vector3.up * (Top[t] + 0.35f), new Vector3(hs * 2, 0.7f, 0.6f), "MudBrick", true, 1.5f, new Vector3(0, f * 90, 0));
                    // Buttress pilasters (Ur style) and lion reliefs between them.
                    int count = Mathf.FloorToInt(hs * 2 / 4f);
                    for (int i = 0; i <= count; i++)
                    {
                        float x = -hs + i * (hs * 2 / count);
                        if (f == 0 && Mathf.Abs(x) < 4.5f) continue; // keep the central stair clear
                        Box(deco, $"Pilaster{t}{f}{i}", outward * (hs + 0.3f) + along * x + Vector3.up * (Bottom[t] + h * 0.5f), new Vector3(0.9f, h, 0.6f), "MudBrick", false, 2f, new Vector3(0, f * 90, 0));
                        if (i < count && (i % 2 == 0))
                        {
                            float lx = x + hs / count;
                            if (f == 0 && Mathf.Abs(lx) < 5f) continue;
                            Box(deco, $"Lion{t}{f}{i}", outward * (hs + 0.14f) + along * lx + Vector3.up * bandY, new Vector3(1.4f, 0.8f, 0.08f), "GoldBright", false, 0.5f, new Vector3(0, f * 90, 0));
                        }
                    }
                }
            }
            MeshGen.CombineByMaterial(deco.gameObject, "ZigDecor");

            // Central monumental staircase (ground -> summit) and two side stairs converging on the first terrace.
            Stairs(z, "CentralStair", new Vector3(0, 0, -37f), Vector3.forward, Summit, 27f, 6f, "Sandstone");
            Stairs(z, "WestStair", new Vector3(-15.5f, 0, -24.5f), Vector3.right, 8f, 11f, 5f, "Sandstone");
            Stairs(z, "EastStair", new Vector3(15.5f, 0, -24.5f), Vector3.left, 8f, 11f, 5f, "Sandstone");
            // Balustrades along the central stair.
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                float ang = Mathf.Atan2(Summit, 27f) * Mathf.Rad2Deg;
                Box(z, "Balustrade" + sgn, new Vector3(sgn * 3.3f, Summit * 0.5f + 0.5f, -23.5f - 0.3f), new Vector3(0.6f, 1.0f, Mathf.Sqrt(27 * 27 + Summit * Summit)), "SandstoneDark", true, 1.5f, new Vector3(-ang, 0, 0));
            }
            // Torches up the stairs and on the terraces.
            for (int i = 0; i < 6; i++)
            {
                float zz = -35f + i * 4.6f;
                float y = Summit * (zz + 37f + 0.9f) / 27f;
                for (int sgn = -1; sgn <= 1; sgn += 2) Place("Torch", z, new Vector3(sgn * 3.3f, y + 0.55f, zz), 0, 0.8f);
            }
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Place("Torch", z, new Vector3(sgn * 20.5f, 8.7f, -20.5f));
                Place("Torch", z, new Vector3(sgn * 14.5f, 15.7f, -14.5f));
                Place("Torch", z, new Vector3(sgn * 3f, Summit + 0.7f, -9f));
            }
        }

        /// <summary>Summit tier built from four blocks around the hidden shaft, plus the shaft floor.</summary>
        static void BoxWithHole(Transform parent, string name, float y0, float y1, float hs, string mat)
        {
            float h = y1 - y0, cy = (y0 + y1) / 2;
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            Box(root, "West", new Vector3((-hs + HX0) / 2, cy, 0), new Vector3(HX0 + hs, h, hs * 2), mat, true, 2.5f);
            Box(root, "East", new Vector3((HX1 + hs) / 2, cy, 0), new Vector3(hs - HX1, h, hs * 2), mat, true, 2.5f);
            Box(root, "South", new Vector3((HX0 + HX1) / 2, cy, (-hs + HZ0) / 2), new Vector3(HX1 - HX0, h, HZ0 + hs), mat, true, 2.5f);
            Box(root, "North", new Vector3((HX0 + HX1) / 2, cy, (HZ1 + hs) / 2), new Vector3(HX1 - HX0, h, hs - HZ1), mat, true, 2.5f);
            Box(root, "ShaftFloor", new Vector3((HX0 + HX1) / 2, Summit - 4.25f, (HZ0 + HZ1) / 2), new Vector3(HX1 - HX0, 0.5f, HZ1 - HZ0), "CrackedFloor", true, 2f);
            Box(root, "ShaftFill", new Vector3((HX0 + HX1) / 2, (y0 + Summit - 4.5f) / 2, (HZ0 + HZ1) / 2), new Vector3(HX1 - HX0, Summit - 4.5f - y0, HZ1 - HZ0), mat, true, 2.5f);
        }

        static void Shrine(Transform world)
        {
            var sh = new GameObject("Shrine").transform; sh.SetParent(world, false);
            float y = Summit, H = 7f, hs = 7f, t = 0.5f;
            // Outer walls (door gap on the south).
            Box(sh, "WallN", new Vector3(0, y + H / 2, hs - t / 2), new Vector3(hs * 2, H, t), "MudBrick", true, 2f);
            Box(sh, "WallW", new Vector3(-hs + t / 2, y + H / 2, 0), new Vector3(t, H, hs * 2), "MudBrick", true, 2f);
            Box(sh, "WallE", new Vector3(hs - t / 2, y + H / 2, 0), new Vector3(t, H, hs * 2), "MudBrick", true, 2f);
            Box(sh, "WallSL", new Vector3(-(hs + 1.5f) / 2, y + H / 2, -hs + t / 2), new Vector3(hs - 1.5f, H, t), "MudBrick", true, 2f);
            Box(sh, "WallSR", new Vector3((hs + 1.5f) / 2, y + H / 2, -hs + t / 2), new Vector3(hs - 1.5f, H, t), "MudBrick", true, 2f);
            Box(sh, "Lintel", new Vector3(0, y + 4.5f + (H - 4.5f) / 2, -hs + t / 2), new Vector3(3f, H - 4.5f, t), "MudBrick", true, 2f);
            Box(sh, "LintelGold", new Vector3(0, y + 4.6f, -hs - 0.05f), new Vector3(3.6f, 0.25f, 0.2f), "Gold", false, 1f);
            Box(sh, "Roof", new Vector3(0, y + H + 0.25f, 0), new Vector3(hs * 2 + 0.6f, 0.5f, hs * 2 + 0.6f), "MudBrick", true, 2.5f);
            Box(sh, "RoofBand", new Vector3(0, y + H - 0.6f, -hs - 0.04f), new Vector3(hs * 2, 0.9f, 0.1f), "LapisTile", false, 1.2f);
            // Lapis interior lining.
            float inner = hs - t - 0.03f;
            Box(sh, "LiningN", new Vector3(0, y + H / 2, inner), new Vector3(inner * 2, H, 0.05f), "LapisTile", false, 1.5f);
            Box(sh, "LiningW", new Vector3(-inner, y + H / 2, 0), new Vector3(0.05f, H, inner * 2), "LapisTile", false, 1.5f);
            Box(sh, "LiningE", new Vector3(inner, y + H / 2, 0), new Vector3(0.05f, H, inner * 2), "LapisTile", false, 1.5f);
            // Floor tiles around the hidden shaft.
            float fy = y + 0.025f;
            Box(sh, "FloorW", new Vector3((-inner + HX0) / 2, fy, 0), new Vector3(HX0 + inner, 0.05f, inner * 2), "CrackedFloor", false, 2f);
            Box(sh, "FloorE", new Vector3((HX1 + inner) / 2, fy, 0), new Vector3(inner - HX1, 0.05f, inner * 2), "CrackedFloor", false, 2f);
            Box(sh, "FloorS", new Vector3((HX0 + HX1) / 2, fy, (-inner + HZ0) / 2), new Vector3(HX1 - HX0, 0.05f, HZ0 + inner), "CrackedFloor", false, 2f);
            Box(sh, "FloorN", new Vector3((HX0 + HX1) / 2, fy, (HZ1 + inner) / 2), new Vector3(HX1 - HX0, 0.05f, inner - HZ1), "CrackedFloor", false, 2f);

            foreach (var p in new[] { new Vector3(-5.2f, y, -4.5f), new Vector3(5.2f, y, -4.5f), new Vector3(-5.2f, y, 5.2f), new Vector3(5.2f, y, 5.2f) })
                PropPrefabs.BuildPillar(sh, "Pillar", p, 5.2f);

            Place("Lamassu", sh, new Vector3(2.2f, y, 4.4f), 180, 0.9f);
            Box(sh, "Altar", new Vector3(2.5f, y + 0.5f, -0.5f), new Vector3(1.6f, 1.0f, 1.0f), "CarvedWall", true, 1f);
            Box(sh, "AltarTop", new Vector3(2.5f, y + 1.04f, -0.5f), new Vector3(1.8f, 0.08f, 1.2f), "Gold", false, 1f);
            var tablet = Place("ClayTablet", sh, new Vector3(2.5f, y + 1.08f, -0.5f), 180);
            var clue = tablet.GetComponent<ClueTablet>();
            clue.title = "A Clay Tablet";
            clue.text = "\"Where the lion bows its head,\nthe way below is opened.\"\n\n<size=22>- inscription of the priests of Marduk</size>";
            clue.objectiveAfter = "Find the lion that bows its head";

            // The sliding floor slab over the hidden stairs.
            var slab = Box(sh, "FloorSlab", new Vector3((HX0 + HX1) / 2, y + 0.15f, (HZ0 + HZ1) / 2), new Vector3(HX1 - HX0 + 0.1f, 0.3f, HZ1 - HZ0 + 0.1f), "CarvedWall", true, 1.5f);
            var door = slab.AddComponent<StoneDoor>(); door.openOffset = new Vector3(2.6f, 0, 0); door.duration = 3f;

            // Four lion reliefs; only the one with its head bowed (east wall, north) works.
            var reliefs = new[]
            {
                PropPrefabs.BuildLionRelief(sh, "LionRelief_W1", new Vector3(-inner + 0.02f, y + 2.4f, -3.8f), 90, false),
                PropPrefabs.BuildLionRelief(sh, "LionRelief_W2", new Vector3(-inner + 0.02f, y + 2.4f, 3.2f), 90, false),
                PropPrefabs.BuildLionRelief(sh, "LionRelief_E1", new Vector3(inner - 0.02f, y + 2.4f, -3.8f), -90, false),
                PropPrefabs.BuildLionRelief(sh, "LionRelief_E2", new Vector3(inner - 0.02f, y + 2.4f, 3.2f), -90, true),
            };
            foreach (var r in reliefs) r.GetComponent<LionRelief>().slab = door;

            // Hidden stairs down to the maze.
            Stairs(sh, "HiddenStairs", new Vector3((HX0 + HX1) / 2, y - 4f, HZ0 + 4.4f), Vector3.back, 4f, 4.4f, HX1 - HX0 - 0.1f, "Sandstone");
            var exit = Trigger(sh, "ToMaze", new Vector3((HX0 + HX1) / 2, y - 3.1f, HZ1 - 0.8f), new Vector3(2.4f, 2f, 1.4f));
            exit.gameObject.AddComponent<SceneExitTrigger>().sceneName = "02_Maze";
            PointLight(sh, "ShaftGlow", new Vector3((HX0 + HX1) / 2, y - 2.5f, HZ1 - 1f), new Color(1f, 0.5f, 0.2f), 1.2f, 5f);

            // Lighting inside the shrine.
            Place("WallTorch", sh, new Vector3(0, y + 2.8f, inner - 0.02f), 180);
            Place("WallTorch", sh, new Vector3(-inner + 0.02f, y + 4.2f, 0), 90);
            Place("WallTorch", sh, new Vector3(inner - 0.02f, y + 4.2f, 0), -90);
            Place("Brazier", sh, new Vector3(-1.0f, y, 6.0f));
            Place("Brazier", sh, new Vector3(4.8f, y, 1.6f));
            var mood = PointLight(sh, "ShrineFill", new Vector3(0, y + 5.5f, 0), new Color(1f, 0.7f, 0.45f), 1.4f, 14f, true);
            mood.shadowStrength = 0.7f;

            var enter = Trigger(sh, "ShrineEntrance", new Vector3(0, y + 1.5f, -5.6f), new Vector3(3f, 3f, 1.2f)).gameObject.AddComponent<StoryTrigger>();
            enter.subtitle = "The shrine of Marduk. Something skitters in the dark... Blind spiders hunt by sound - crouch (C) to move silently.";
            enter.objective = "Find the hidden entrance";
            enter.seconds = 7f;

            // A blind spider guards the shrine (patrols near the true relief).
            var area = new GameObject("ShrineArea").AddComponent<OpenArea>();
            area.transform.SetParent(sh, false);
            area.transform.localPosition = new Vector3(0, y, 0);
            area.patrolCenter = new Vector3(3.2f, 0, 1.2f); area.patrolSize = new Vector3(4.5f, 0, 7.5f);
            area.roamCenter = Vector3.zero; area.roamSize = new Vector3(12f, 0, 12f);
            var spider = Place("Spider", sh, new Vector3(4f, y + 0.1f, 2.5f), 180, 0.75f);
            var ai = spider.GetComponent<SpiderAI>(); ai.area = area; ai.huntSpeed = 3.6f; ai.patrolSpeed = 1.2f;
        }

        static void Approach(Transform world)
        {
            // Ishtar-style gate across the road.
            var g = new GameObject("IshtarGate").transform; g.SetParent(world, false); g.localPosition = new Vector3(0, 0, -92f);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(g, "Tower" + s, new Vector3(s * 5.5f, 5f, 0), new Vector3(4f, 10f, 4.5f), "LapisTile", true, 1.6f);
                Box(g, "Crenel" + s, new Vector3(s * 5.5f, 10.4f, 0), new Vector3(4.4f, 0.8f, 4.9f), "SandstoneDark", true, 1.5f);
                for (int k = 0; k < 3; k++)
                    Box(g, $"Lion{s}{k}", new Vector3(s * 5.5f, 2.5f + k * 2.6f, -2.3f), new Vector3(2.2f, 1.1f, 0.1f), "GoldBright", false, 0.6f);
            }
            Box(g, "Lintel", new Vector3(0, 8.6f, 0), new Vector3(7f, 2.8f, 4.5f), "LapisTile", true, 1.6f);
            Box(g, "LintelTrim", new Vector3(0, 7.15f, -2.3f), new Vector3(7f, 0.2f, 0.15f), "Gold", false, 1f);

            for (int i = 0; i < 9; i++)
                for (int s = -1; s <= 1; s += 2)
                    Place("Torch", world, new Vector3(s * 4.6f, 0, -100f + i * 7.5f));
            // Ruined walls beside the road.
            var rnd = new System.Random(7);
            for (int i = 0; i < 8; i++)
            {
                float sx = (i % 2 == 0 ? -1 : 1) * (10 + (float)rnd.NextDouble() * 8);
                float sz = -50 - i * 7 - (float)rnd.NextDouble() * 3;
                float h = 0.8f + (float)rnd.NextDouble() * 2.5f;
                Box(world, "Ruin" + i, new Vector3(sx, h / 2, sz), new Vector3(1f, h, 3 + (float)rnd.NextDouble() * 4), "MudBrick", true, 2f, new Vector3(0, (float)rnd.NextDouble() * 20 - 10, 0));
            }
            var welcome = Trigger(world, "GateStory", new Vector3(0, 1.5f, -92f), new Vector3(7, 3, 2)).gameObject.AddComponent<StoryTrigger>();
            welcome.subtitle = "Babylon. Somewhere beneath this ziggurat lies the Hidden Sumerian Tablet.";
        }

        static void Bounds(Transform world)
        {
            var b = new GameObject("Bounds").transform; b.SetParent(world, false);
            Invisible(b, "N", new Vector3(0, 10, 60), new Vector3(170, 40, 1));
            Invisible(b, "S", new Vector3(0, 10, -125), new Vector3(170, 40, 1));
            Invisible(b, "E", new Vector3(80, 10, -30), new Vector3(1, 40, 190));
            Invisible(b, "W", new Vector3(-80, 10, -30), new Vector3(1, 40, 190));
        }
    }
}
