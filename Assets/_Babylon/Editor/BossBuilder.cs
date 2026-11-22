using UnityEngine;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>04_Boss: circular cavern-temple, six unbreakable pillars, the sleeping Stone Master and the sealed door.</summary>
    public static class BossBuilder
    {
        const float R = 15.5f, WallH = 12f;

        public static void Build()
        {
            var s = SceneCommon.New("04_Boss");
            SceneCommon.Lighting(null, new Color(0.2f, 0.22f, 0.28f), new Color(0.14f, 0.15f, 0.18f), new Color(0.07f, 0.07f, 0.08f),
                new Color(0.05f, 0.06f, 0.09f), 0.022f);
            // Soft shadowless moonlight fill so the arena, the pillars and the boss read clearly.
            SceneCommon.Sun(new Vector3(65, -20, 0), new Color(0.65f, 0.72f, 0.9f), 0.5f, false).name = "MoonFill";
            SceneCommon.Volume("Vol_Boss");
            SceneCommon.Info(4, "Round 3 - The Boss Fight", "Defeat the Guardian of Babylon", MusicId.Tense, SoundId.CaveDrips,
                "Approach the kneeling idol...",
                "An idol of living stone kneels in the dark, spear in hand: the Stone Master. Strike its glowing core when it is exposed - and never stand still.", true, 0.5f);

            var arena = new GameObject("Arena").transform;
            var floor = Mesh(arena, "Floor", MeshGen.Cylinder(R + 0.5f, 0.4f, 48, 3f), "CrackedFloor", new Vector3(0, -0.2f, 0));
            floor.AddComponent<MeshCollider>().sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
            Mesh(arena, "Ceiling", MeshGen.Cylinder(R + 1f, 0.6f, 48, 4f), "CaveRock", new Vector3(0, WallH + 0.3f, 0)).layer = Layers.Ceiling;

            // Ring wall with relief panels; gaps for the south entrance and the north door.
            int segs = 28;
            for (int i = 0; i < segs; i++)
            {
                float a = i * 360f / segs;
                Vector3 dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                if (Mathf.Abs(Mathf.DeltaAngle(a, 180)) < 5 || Mathf.Abs(Mathf.DeltaAngle(a, 0)) < 5) continue;
                Box(arena, "Wall" + i, dir * (R + 0.6f) + Vector3.up * WallH / 2, new Vector3(3.6f, WallH, 1.2f), "MossRock", true, 2.5f, new Vector3(0, a, 0));
                string panel = i % 3 == 0 ? "LapisTile" : "CarvedWall";
                Box(arena, "Relief" + i, dir * (R - 0.02f) + Vector3.up * 3.5f, new Vector3(2.6f, 4f, 0.12f), panel, false, 1.4f, new Vector3(0, a, 0));
                if (i % 2 == 0) Box(arena, "Trim" + i, dir * (R - 0.05f) + Vector3.up * 5.7f, new Vector3(3f, 0.2f, 0.15f), "Gold", false, 1f, new Vector3(0, a, 0));
            }
            // Stalactites.
            var rnd = new System.Random(4);
            for (int i = 0; i < 30; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2, r = (float)rnd.NextDouble() * (R - 1);
                float len = 1f + (float)rnd.NextDouble() * 2.5f;
                Cyl(arena, "Stalactite" + i, new Vector3(Mathf.Cos(a) * r, WallH - len / 2, Mathf.Sin(a) * r), 0.35f, len, "CaveRock", false, new Vector3(180, 0, 0), 8, 0.02f);
            }

            // Six sturdy pillars (tag Pillar). They never break, so the fight can't soft-lock.
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var p = Place("StonePillar", arena, new Vector3(Mathf.Cos(a) * 9f, 0, Mathf.Sin(a) * 9f));
                p.transform.LookAt(new Vector3(0, 0, 0));
                p.transform.Rotate(0, 0, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                Place("Brazier", arena, new Vector3(Mathf.Cos(a) * 13.2f, 0, Mathf.Sin(a) * 13.2f));
            }

            // Moonlight shaft from a crack in the ceiling.
            var beam = new GameObject("MoonShaft"); beam.transform.SetParent(arena, false);
            beam.transform.SetPositionAndRotation(new Vector3(0, WallH - 0.5f, 2f), Quaternion.Euler(90, 0, 0));
            var spot = beam.AddComponent<Light>();
            spot.type = LightType.Spot; spot.color = new Color(0.55f, 0.7f, 1f); spot.intensity = 14f; spot.range = 18f; spot.spotAngle = 70f;
            spot.shadows = LightShadows.Soft;
            var dust = Place("FX_FallingDust", arena, new Vector3(0, WallH - 1, 2f));
            dust.transform.localScale = Vector3.one * 0.6f;

            // Entrance corridor (south).
            var ent = new GameObject("Entrance").transform; ent.SetParent(arena, false);
            Box(ent, "Floor", new Vector3(0, -0.1f, -R - 4), new Vector3(4, 0.2f, 9), "CrackedFloor", true, 3f);
            Box(ent, "W", new Vector3(-2.2f, 3, -R - 4), new Vector3(0.4f, 6, 9), "CaveRock", true, 3f);
            Box(ent, "E", new Vector3(2.2f, 3, -R - 4), new Vector3(0.4f, 6, 9), "CaveRock", true, 3f);
            Box(ent, "Back", new Vector3(0, 3, -R - 8.6f), new Vector3(4.8f, 6, 0.4f), "CaveRock", true, 3f);
            Box(ent, "Ceil", new Vector3(0, 6.1f, -R - 4), new Vector3(4.8f, 0.3f, 9), "CaveRock", true, 3f).layer = Layers.Ceiling;
            Box(ent, "Above", new Vector3(0, 9, -R - 0.6f), new Vector3(4.2f, 6, 1.2f), "MossRock", true, 2.5f);
            Place("WallTorch", ent, new Vector3(-2f, 3f, -R - 3), 90);
            Place("WallTorch", ent, new Vector3(2f, 3f, -R - 3), -90);

            // Sealed north door and the passage to the final chamber.
            var door = Box(arena, "SealedDoor", new Vector3(0, 3f, R + 0.3f), new Vector3(3.8f, 6f, 0.8f), "CarvedWall", true, 1.5f);
            Box(door.transform, "Seal", new Vector3(0, 0.3f, -0.42f), new Vector3(1.2f, 1.2f, 0.05f), "GoldBright", false, 0.6f, new Vector3(0, 0, 45));
            var sd = door.AddComponent<StoneDoor>(); sd.openOffset = new Vector3(0, 5.9f, 0); sd.duration = 4f;
            Box(arena, "DoorAbove", new Vector3(0, 9, R + 0.6f), new Vector3(4.2f, 6, 1.2f), "MossRock", true, 2.5f);
            var pass = new GameObject("Passage").transform; pass.SetParent(arena, false);
            Box(pass, "Floor", new Vector3(0, -0.1f, R + 5), new Vector3(4, 0.2f, 9), "CrackedFloor", true, 3f);
            Box(pass, "W", new Vector3(-2.2f, 3, R + 5), new Vector3(0.4f, 6, 9), "CaveRock", true, 3f);
            Box(pass, "E", new Vector3(2.2f, 3, R + 5), new Vector3(0.4f, 6, 9), "CaveRock", true, 3f);
            Box(pass, "Ceil", new Vector3(0, 6.1f, R + 5), new Vector3(4.8f, 0.3f, 9), "CaveRock", true, 3f).layer = Layers.Ceiling;
            Box(pass, "End", new Vector3(0, 3, R + 9.6f), new Vector3(4.8f, 6, 0.4f), "CaveRock", true, 3f);
            PointLight(pass, "GoldenGlow", new Vector3(0, 3, R + 8), new Color(1f, 0.75f, 0.35f), 4f, 10f);
            Trigger(pass, "ToFinal", new Vector3(0, 1.5f, R + 7.5f), new Vector3(3.6f, 3, 2)).gameObject.AddComponent<SceneExitTrigger>().sceneName = "05_FinalChamber";

            // The idol's pedestal and the Stone Master.
            Box(arena, "Pedestal1", new Vector3(0, 0.2f, 8.2f), new Vector3(5.5f, 0.4f, 4.5f), "MossRock", true, 2f);
            Box(arena, "Pedestal2", new Vector3(0, 0.6f, 8.4f), new Vector3(4.2f, 0.4f, 3.6f), "CarvedWall", true, 2f);
            var boss = Place("StoneMaster", null, new Vector3(0, 0.85f, 8.4f), 180);
            var ai = boss.GetComponent<StoneMasterAI>();
            ai.arenaCenter = Vector3.zero; ai.arenaRadius = R - 1f; ai.exitDoor = sd; ai.wakeRadius = 11f;

            var tip = Trigger(arena, "Tutorial", new Vector3(0, 1.5f, -9f), new Vector3(10, 3, 1)).gameObject.AddComponent<StoryTrigger>();
            tip.subtitle = "Duck (Ctrl) under its sweeps, jump (Space) its shockwaves, roll (Q) from its charge - and lure it into a pillar.";
            tip.seconds = 7f;

            SceneCommon.ReflectionProbe(new Vector3(0, 4, 0), new Vector3(34, 14, 34));
            SceneCommon.Player(new Vector3(0, 0.05f, -R - 6f), 0, false);
            SceneCommon.UI();
            SceneCommon.Save(s, "04_Boss");
        }
    }
}
