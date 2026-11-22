using UnityEngine;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>05_FinalChamber: golden hall of tablets, lamassu statues, star-studded lapis walls, the Sumerian tablet.</summary>
    public static class FinalBuilder
    {
        const float HW = 6.5f, L = 30f, H = 9f;

        public static void Build()
        {
            var s = SceneCommon.New("05_FinalChamber");
            SceneCommon.Lighting(null, new Color(0.3f, 0.24f, 0.16f), new Color(0.22f, 0.16f, 0.1f), new Color(0.08f, 0.06f, 0.04f),
                new Color(0.25f, 0.18f, 0.1f), 0.018f);
            SceneCommon.Volume("Vol_Final");
            SceneCommon.Info(5, "The Final Chamber", "Claim the Hidden Sumerian Tablet", MusicId.Triumphant, SoundId.None,
                "Claim the Hidden Sumerian Tablet",
                "The Hall of Tablets. Here the first scribes of Sumer pressed their words into clay, for the ages to come.", true);

            var hall = new GameObject("FinalChamber").transform;
            Box(hall, "Floor", new Vector3(0, -0.15f, L / 2), new Vector3(HW * 2, 0.3f, L), "CrackedFloor", true, 3f);
            Box(hall, "Runner", new Vector3(0, 0.01f, 12f), new Vector3(2.2f, 0.02f, 20f), "LapisTile", false, 1.2f);
            Box(hall, "RunnerTrimL", new Vector3(-1.15f, 0.015f, 12f), new Vector3(0.1f, 0.03f, 20f), "Gold", false, 1f);
            Box(hall, "RunnerTrimR", new Vector3(1.15f, 0.015f, 12f), new Vector3(0.1f, 0.03f, 20f), "Gold", false, 1f);
            Box(hall, "Ceiling", new Vector3(0, H + 0.25f, L / 2), new Vector3(HW * 2, 0.5f, L), "LapisStars", true, 2.5f).layer = Layers.Ceiling;
            Box(hall, "WallW", new Vector3(-HW - 0.25f, H / 2, L / 2), new Vector3(0.5f, H, L), "LapisStars", true, 2.5f);
            Box(hall, "WallE", new Vector3(HW + 0.25f, H / 2, L / 2), new Vector3(0.5f, H, L), "LapisStars", true, 2.5f);
            Box(hall, "WallN", new Vector3(0, H / 2, L + 0.25f), new Vector3(HW * 2 + 1, H, 0.5f), "LapisStars", true, 2.5f);
            Box(hall, "WallS", new Vector3(0, H / 2, -0.25f), new Vector3(HW * 2 + 1, H, 0.5f), "LapisStars", true, 2.5f);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Box(hall, "Frieze" + sgn, new Vector3(sgn * (HW - 0.04f), 6.5f, L / 2), new Vector3(0.08f, 1.0f, L), "LapisTile", false, 1f);
                Box(hall, "Gold" + sgn, new Vector3(sgn * (HW - 0.05f), 7.1f, L / 2), new Vector3(0.1f, 0.15f, L), "Gold", false, 1f);
                Box(hall, "Base" + sgn, new Vector3(sgn * (HW - 0.06f), 0.3f, L / 2), new Vector3(0.12f, 0.6f, L), "Gold", false, 1f);
            }
            // Great relief wall behind the pedestal.
            Box(hall, "Relief", new Vector3(0, 4.2f, L - 0.08f), new Vector3(8, 6.5f, 0.12f), "CarvedWall", false, 1.6f);
            Box(hall, "ReliefFrame", new Vector3(0, 4.2f, L - 0.04f), new Vector3(8.6f, 7.1f, 0.06f), "Gold", false, 1f);

            // Statues flanking the path.
            for (int i = 0; i < 3; i++)
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    float z = 6 + i * 6f;
                    Place("Lamassu", hall, new Vector3(sgn * 4.2f, 0, z), sgn < 0 ? 90 : -90, 0.75f);
                    Place("Torch", hall, new Vector3(sgn * 2.2f, 0, z + 3f), 0, 0.85f);
                }

            // Stepped pedestal with the tablet.
            float pz = 24.5f;
            Box(hall, "Step1", new Vector3(0, 0.2f, pz), new Vector3(4.4f, 0.4f, 4.4f), "CarvedWall", true, 1.5f);
            Box(hall, "Step2", new Vector3(0, 0.6f, pz), new Vector3(3.2f, 0.4f, 3.2f), "LapisTile", true, 1.2f);
            Box(hall, "Step3", new Vector3(0, 1.0f, pz), new Vector3(2f, 0.4f, 2f), "Gold", true, 1f);
            Box(hall, "Altar", new Vector3(0, 1.55f, pz), new Vector3(1f, 0.7f, 0.8f), "CarvedWall", true, 1f);
            Place("SumerianTablet", hall, new Vector3(0, 1.92f, pz), 180);
            var spot = new GameObject("TabletSpot"); spot.transform.SetParent(hall, false);
            spot.transform.SetPositionAndRotation(new Vector3(0, H - 0.4f, pz - 1.5f), Quaternion.Euler(75, 0, 0));
            var sl = spot.AddComponent<Light>(); sl.type = LightType.Spot; sl.color = new Color(1f, 0.82f, 0.5f); sl.intensity = 12f; sl.range = 14f; sl.spotAngle = 40f;
            sl.shadows = LightShadows.Soft;
            Place("Brazier", hall, new Vector3(-2.8f, 0, pz - 2.6f));
            Place("Brazier", hall, new Vector3(2.8f, 0, pz - 2.6f));
            for (int i = 0; i < 4; i++) PointLight(hall, "GoldFill" + i, new Vector3(0, 6.5f, 4 + i * 7f), new Color(1f, 0.78f, 0.45f), 1.6f, 11f);
            var motes = Place("FX_DustMotes", hall, new Vector3(0, 3f, L / 2));
            motes.transform.localScale = new Vector3(1f, 1f, 2.2f);

            SceneCommon.ReflectionProbe(new Vector3(0, 3, L / 2), new Vector3(14, 10, 32));
            SceneCommon.Player(new Vector3(0, 0.05f, 1.6f), 0, false);
            SceneCommon.UI(victory: true);
            SceneCommon.Save(s, "05_FinalChamber");
        }
    }
}
