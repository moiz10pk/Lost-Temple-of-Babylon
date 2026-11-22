using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>02_Maze: 12x12 seeded maze beneath the temple with blind spiders, key, urns, locked gate and minimap.</summary>
    public static class MazeBuilder
    {
        const int N = 12; const float C = 4f, WallH = 4.5f, WallT = 0.5f, MapY = 5.2f;

        public static void Build()
        {
            var s = SceneCommon.New("02_Maze");
            SceneCommon.Lighting(null, new Color(0.09f, 0.07f, 0.06f), new Color(0.07f, 0.05f, 0.04f), new Color(0.03f, 0.02f, 0.02f),
                new Color(0.07f, 0.05f, 0.03f), 0.045f);
            SceneCommon.Volume("Vol_Maze");
            SceneCommon.Info(2, "Round 1 - The Maze", "Survive the maze, avoid spiders, find the key and escape", MusicId.Tense, SoundId.CaveDrips,
                "Find the Golden Key (use the map)",
                "Blind spiders roam these halls, hunting by sound alone. Crouch (C) or duck (Ctrl) to move silently - or stand still and let them pass.", false, 0.6f);

            var m = MazeLayout.Generate(N, N, 1750, 0.08f, 3);
            var world = new GameObject("Maze").transform;
            var grid = world.gameObject.AddComponent<MazeGrid>();
            grid.Store(m, C, Vector3.zero);

            var walls = new GameObject("Walls").transform; walls.SetParent(world, false);
            var cols = new GameObject("WallColliders").transform; cols.SetParent(world, false);
            var map = new GameObject("MapGeometry").transform; map.SetParent(world, false);
            int wallIndex = 0;
            var touched = new HashSet<Vector2Int>();
            void Wall(Vector3 center, bool alongX)
            {
                Vector3 half = alongX ? new Vector3(C / 2, 0, 0) : new Vector3(0, 0, C / 2);
                foreach (var e in new[] { center - half, center + half }) touched.Add(new Vector2Int(Mathf.RoundToInt(e.x / C), Mathf.RoundToInt(e.z / C)));
                Vector3 size = alongX ? new Vector3(C + WallT, WallH, WallT) : new Vector3(WallT, WallH, C + WallT);
                bool lapis = wallIndex % 5 == 2;
                Box(walls, "W" + wallIndex, center, size, "CarvedWall", false, 2.25f);
                Vector3 bandSize = alongX ? new Vector3(C - 0.2f, 0.7f, WallT + 0.06f) : new Vector3(WallT + 0.06f, 0.7f, C - 0.2f);
                if (lapis) Box(walls, "Band" + wallIndex, center + Vector3.up * -0.6f, bandSize, "LapisTile", false, 0.7f);
                Box(walls, "Trim" + wallIndex, center + Vector3.up * (WallH / 2 - 0.12f), alongX ? new Vector3(C + WallT, 0.16f, WallT + 0.08f) : new Vector3(WallT + 0.08f, 0.16f, C + WallT), "Gold", false, 1f);
                Invisible(cols, "C" + wallIndex, center, size);
                var top = Box(map, "M" + wallIndex, new Vector3(center.x, MapY - 0.1f, center.z), new Vector3(size.x + 0.2f, 0.05f, size.z + 0.2f), "MapWall", false);
                wallIndex++;
            }
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (y == 0) Wall(new Vector3((x + 0.5f) * C, WallH / 2, 0), true);
                    if (x == 0 && cell != m.start) Wall(new Vector3(0, WallH / 2, (y + 0.5f) * C), false);
                    if (m.wallE[m.Idx(x, y)] && cell != m.exit) Wall(new Vector3((x + 1) * C, WallH / 2, (y + 0.5f) * C), false);
                    if (m.wallN[m.Idx(x, y)]) Wall(new Vector3((x + 0.5f) * C, WallH / 2, (y + 1) * C), true);
                }
            // Corner posts wherever walls meet.
            foreach (var v in touched)
                Box(walls, $"Post{v.x}_{v.y}", new Vector3(v.x * C, WallH / 2, v.y * C), new Vector3(0.75f, WallH + 0.1f, 0.75f), "SandstoneDark", false, 2f);
            MeshGen.CombineByMaterial(walls.gameObject, "MazeWalls");
            MeshGen.CombineByMaterial(map.gameObject, "MapWalls");
            Layers.SetRecursive(map.gameObject, Layers.Minimap);

            Box(world, "Floor", new Vector3(N * C / 2, -0.1f, N * C / 2), new Vector3(N * C + 2, 0.2f, N * C + 2), "CrackedFloor", true, 3f);
            var ceil = Box(world, "Ceiling", new Vector3(N * C / 2, WallH + 0.25f, N * C / 2), new Vector3(N * C + 2, 0.5f, N * C + 2), "CaveRock", true, 4f);
            ceil.layer = Layers.Ceiling;
            var mapFloor = Box(world, "MapFloor", new Vector3(N * C / 2 + 3, MapY - 0.2f, N * C / 2), new Vector3(N * C + 14, 0.05f, N * C + 6), "MapFloor", false);
            mapFloor.layer = Layers.Minimap;

            StartAlcove(world);
            ExitChamber(world);
            Torches(world, m);
            Rooms(world, m);
            Spiders(world, m, grid);
            Minimap(world);
            SceneCommon.ReflectionProbe(new Vector3(24, 2, 24), new Vector3(60, 8, 60));

            var player = SceneCommon.Player(new Vector3(-2.5f, 0.05f, 2f), 90, true);
            var arrow = Mesh(world, "MapPlayer", MeshGen.Arrow(), "MapPlayer", new Vector3(0, MapY + 0.1f, 0), null, Vector3.one * 1.4f);
            arrow.layer = Layers.Minimap;
            var mk = arrow.AddComponent<MinimapMarker>(); mk.target = player.transform; mk.rotateWithTarget = true; mk.height = MapY + 0.1f;
            var hud = SceneCommon.UI();
            SceneCommon.SetMinimap(hud, AssetGenerator.MinimapTexture());
            SceneCommon.Save(s, "02_Maze");
        }

        static void StartAlcove(Transform world)
        {
            var a = new GameObject("StartAlcove").transform; a.SetParent(world, false);
            Box(a, "Floor", new Vector3(-4, -0.1f, 2), new Vector3(8, 0.2f, 4.5f), "CrackedFloor", true, 3f);
            Box(a, "WallS", new Vector3(-4, WallH / 2, -0.25f), new Vector3(8, WallH, WallT), "CarvedWall", true, 2.25f);
            Box(a, "WallN", new Vector3(-4, WallH / 2, 4.25f), new Vector3(8, WallH, WallT), "CarvedWall", true, 2.25f);
            Box(a, "Ceiling", new Vector3(-4, WallH + 0.25f, 2), new Vector3(8.5f, 0.5f, 5), "CaveRock", true, 3f).layer = Layers.Ceiling;
            // The stairs we came down (now blocked by rubble).
            Stairs(a, "StairsUp", new Vector3(-5.5f, 0, 2), Vector3.left, 3.2f, 3.6f, 3.4f, "Sandstone");
            for (int i = 0; i < 6; i++) Place("Rock" + (1 + i % 3), a, new Vector3(-7.4f + (i % 3) * 0.3f, 2.4f + (i / 3) * 0.8f, 0.9f + i * 0.45f), i * 50, 1.3f);
            Box(a, "BackWall", new Vector3(-8.25f, WallH / 2 + 2, 2), new Vector3(WallT, WallH + 4, 4.5f), "CarvedWall", true, 2.25f);
            Place("WallTorch", a, new Vector3(-3, 2.6f, 3.98f), 180);
            var st = Trigger(a, "Story", new Vector3(-1f, 1.5f, 2f), new Vector3(1, 3, 4)).gameObject.AddComponent<StoryTrigger>();
            st.subtitle = "The way back is sealed. Watch the map in the corner - and listen for the skittering of legs.";
        }

        static void ExitChamber(Transform world)
        {
            var e = new GameObject("ExitChamber").transform; e.SetParent(world, false);
            float z0 = (N - 1) * C, zc = z0 + C / 2, x0 = N * C;
            Box(e, "Floor", new Vector3(x0 + 5, -0.1f, zc), new Vector3(10, 0.2f, C + 1), "CrackedFloor", true, 3f);
            Box(e, "WallS", new Vector3(x0 + 5, 5, z0 - 0.25f), new Vector3(10.5f, 10, WallT), "CarvedWall", true, 2.25f);
            Box(e, "WallN", new Vector3(x0 + 5, 5, z0 + C + 0.25f), new Vector3(10.5f, 10, WallT), "CarvedWall", true, 2.25f);
            Box(e, "WallE", new Vector3(x0 + 10.25f, 5, zc), new Vector3(WallT, 10, C + 1), "CarvedWall", true, 2.25f);
            Box(e, "Above", new Vector3(x0, WallH + 2.75f, zc), new Vector3(WallT, 5.5f, C + 0.5f), "CarvedWall", true, 2.25f);
            Box(e, "Ceiling", new Vector3(x0 + 5, 10.25f, zc), new Vector3(10.5f, 0.5f, C + 1), "CaveRock", true, 3f).layer = Layers.Ceiling;
            Stairs(e, "StairsUp", new Vector3(x0 + 1.5f, 0, zc), Vector3.right, 4.5f, 6.5f, C - 0.2f, "Sandstone");
            Box(e, "Landing", new Vector3(x0 + 9, 4.25f, zc), new Vector3(2.5f, 0.5f, C), "Sandstone", true, 2f);
            Place("WallTorch", e, new Vector3(x0 + 4, 6.2f, z0 + C - 0.02f), 180);
            Place("WallTorch", e, new Vector3(x0 + 4, 6.2f, z0 + 0.02f), 0);
            PointLight(e, "Daylight", new Vector3(x0 + 9, 8, zc), new Color(1f, 0.85f, 0.6f), 3f, 12f);
            var trig = Trigger(e, "ToRiddles", new Vector3(x0 + 9, 5.8f, zc), new Vector3(2, 3, C));
            trig.gameObject.AddComponent<SceneExitTrigger>().sceneName = "03_Riddles";

            // Locked bronze gate in the maze's east wall.
            var gate = new GameObject("BronzeGate"); gate.transform.SetParent(e, false);
            gate.transform.localPosition = new Vector3(x0, 0, zc);
            gate.layer = Layers.Interactable;
            var vis = Node("Visual", gate.transform, Vector3.zero).transform;
            for (int i = 0; i < 11; i++) Cyl(vis, "Bar" + i, new Vector3(0, WallH / 2, -1.85f + i * 0.37f), 0.05f, WallH, "Bronze", false, null, 8);
            for (int k = 0; k < 3; k++) Box(vis, "Rail" + k, new Vector3(0, 0.6f + k * 1.6f, 0), new Vector3(0.12f, 0.14f, C), "Bronze", false, 1f);
            Box(vis, "Lock", new Vector3(-0.12f, 1.3f, 0), new Vector3(0.18f, 0.4f, 0.4f), "Gold", false, 0.5f);
            var col = gate.AddComponent<BoxCollider>(); col.size = new Vector3(0.4f, WallH, C); col.center = new Vector3(0, WallH / 2, 0);
            var door = gate.AddComponent<StoneDoor>(); door.openOffset = new Vector3(0, WallH - 0.3f, 0); door.duration = 3f;
            var mg = gate.AddComponent<MazeGate>(); mg.door = door; mg.verb = "unlock the bronze gate";
            var icon = Box(world, "MapExit", new Vector3(x0 + 1.5f, MapY, zc), new Vector3(2.5f, 0.05f, 2.5f), "MapExit", false);
            icon.layer = Layers.Minimap;
        }

        static void Torches(Transform world, MazeLayout m)
        {
            var t = new GameObject("Torches").transform; t.SetParent(world, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if ((x + y) % 2 != 0) continue;
                    Vector3 c = new Vector3((x + 0.5f) * C, 0, (y + 0.5f) * C);
                    float off = C / 2 - WallT / 2 - 0.02f;
                    if (!m.CanMove(x, y, 0)) Place("WallTorch", t, c + new Vector3(0, 2.7f, off), 180);
                    else if (!m.CanMove(x, y, 1)) Place("WallTorch", t, c + new Vector3(off, 2.7f, 0), -90);
                    else if (!m.CanMove(x, y, 2)) Place("WallTorch", t, c + new Vector3(0, 2.7f, -off), 0);
                    else if (!m.CanMove(x, y, 3)) Place("WallTorch", t, c + new Vector3(-off, 2.7f, 0), 90);
                }
        }

        static Vector3 Center(Vector2Int c) => new Vector3((c.x + 0.5f) * C, 0, (c.y + 0.5f) * C);

        static void Rooms(Transform world, MazeLayout m)
        {
            var r = new GameObject("RoomsAndLoot").transform; r.SetParent(world, false);
            // Key altar in the furthest room.
            var room = m.rooms[m.keyRoom];
            var kc = new Vector3((room.xMin + 1) * C, 0, (room.yMin + 1) * C);
            Box(r, "KeyAltar", kc + Vector3.up * 0.45f, new Vector3(1.2f, 0.9f, 0.9f), "CarvedWall", true, 1f);
            Box(r, "KeyAltarTop", kc + Vector3.up * 0.93f, new Vector3(1.35f, 0.08f, 1.05f), "Gold", false, 1f);
            var key = Place("GoldenKey", r, kc + Vector3.up * 0.97f);
            var keyIcon = Box(world, "MapKey", new Vector3(kc.x, MapY, kc.z), new Vector3(1.6f, 0.05f, 1.6f), "MapKey", false);
            keyIcon.layer = Layers.Minimap;
            key.GetComponent<KeyPickup>().mapIcon = keyIcon;
            Place("Brazier", r, kc + new Vector3(-2.2f, 0, -2.2f));
            Place("Brazier", r, kc + new Vector3(2.2f, 0, 2.2f));
            // Healing urns in the other rooms and in dead ends.
            foreach (var c in m.urnCells) Place("HealingUrn", r, Center(c) + new Vector3(0.8f, 0, 0.8f));
            for (int i = 0; i < m.rooms.Count; i++)
            {
                if (i == m.keyRoom) continue;
                var rr = m.rooms[i];
                Place("Lamassu", r, new Vector3((rr.xMin + 1) * C, 0, (rr.yMin + 1) * C), 45, 0.6f);
            }
        }

        static void Spiders(Transform world, MazeLayout m, MazeGrid grid)
        {
            var dist = m.Distances(m.start);
            var rnd = new System.Random(1750);
            var chosen = new List<Vector2Int>();
            for (int tries = 0; tries < 4000 && chosen.Count < 6; tries++)
            {
                var c = new Vector2Int(rnd.Next(N), rnd.Next(N));
                if (dist[m.Idx(c.x, c.y)] < 4 || c.x + c.y < 4) continue;
                bool far = true;
                foreach (var o in chosen) if (Mathf.Abs(o.x - c.x) + Mathf.Abs(o.y - c.y) < 4) far = false;
                if (!far) continue;
                chosen.Add(c);
            }
            var sp = new GameObject("Spiders").transform; sp.SetParent(world, false);
            foreach (var c in chosen)
            {
                var s = Place("Spider", sp, Center(c) + Vector3.up * 0.05f, rnd.Next(4) * 90);
                s.GetComponent<SpiderAI>().area = grid;
            }
        }

        static void Minimap(Transform world)
        {
            var go = new GameObject("MinimapCamera");
            go.transform.SetParent(world, false);
            go.transform.SetPositionAndRotation(new Vector3(N * C / 2 + 2, MapY + 3f, N * C / 2), Quaternion.Euler(90, 0, 0));
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = N * C / 2 + 3f;
            cam.cullingMask = LayerMask.GetMask(Layers.MinimapName);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.035f, 0.02f, 1);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 6f;
            cam.targetTexture = AssetGenerator.MinimapTexture();
            cam.depth = -5;
            var d = go.AddComponent<UniversalAdditionalCameraData>();
            d.renderPostProcessing = false; d.renderShadows = false;
        }
    }
}
