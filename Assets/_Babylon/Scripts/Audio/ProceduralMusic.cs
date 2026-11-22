using System;
using UnityEngine;
using Random = System.Random;

namespace Babylon
{
    /// <summary>Generative loops over a low drone in the Hijaz scale (D, Eb, F#, G, A, Bb, C) for a Mesopotamian feel.</summary>
    public static class ProceduralMusic
    {
        public const int Rate = 22050;
        const float LoopSeconds = 32f, Tail = 3f;
        static readonly int[] Hijaz = { 0, 1, 4, 5, 7, 8, 10 };
        static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 };
        const float D3 = 146.83f;

        static float Note(int[] scale, int degree)
        {
            int oct = Mathf.FloorToInt(degree / 7f);
            int idx = degree - oct * 7;
            return D3 * Mathf.Pow(2f, (scale[idx] + 12 * oct) / 12f);
        }

        public static AudioClip CreateClip(MusicId id)
        {
            var data = Render(id);
            var clip = AudioClip.Create("Music_" + id, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] Render(MusicId id)
        {
            var r = new Random(1750 + (int)id * 31);
            var d = new float[(int)((LoopSeconds + Tail) * Rate)];
            switch (id)
            {
                case MusicId.Calm: Drone(d, 73.4f, 0.16f, r); Oud(d, r, 0.62f, 0.32f, 0, 9, 0.35f); Dsp.Reverb(d, Rate, 0.35f, 1.4f); break;
                case MusicId.Tense: Drone(d, 36.7f, 0.22f, r); Heartbeat(d); Shimmers(d, r); Oud(d, r, 1.6f, 0.6f, -4, 2, 0.22f); Dsp.Reverb(d, Rate, 0.3f, 1.7f); break;
                case MusicId.Mysterious: Drone(d, 73.4f, 0.14f, r); Bells(d, r); Dsp.Reverb(d, Rate, 0.5f, 2f, 0.84f); break;
                case MusicId.Intense: Drone(d, 36.7f, 0.2f, r); Drums(d, r); Ostinato(d, r); Dsp.Reverb(d, Rate, 0.22f, 1.3f); break;
                case MusicId.Triumphant: Pad(d); Melody(d, r); Dsp.Reverb(d, Rate, 0.4f, 1.6f); break;
            }
            for (int i = 0; i < d.Length; i++) d[i] = Dsp.Tanh(d[i] * 1.2f);
            d = Dsp.MakeLoop(d, (int)(Tail * Rate));
            Dsp.Normalize(d, 0.75f);
            return d;
        }

        static void Drone(float[] d, float root, float amp, Random r)
        {
            float p = Dsp.Rand(r, 0, 6);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate, s = 0;
                for (int k = 1; k <= 6; k++) s += Dsp.Sin(Dsp.TwoPi * root * k * t * (1f + 0.0008f * k)) / (k * k * 0.7f + 0.3f);
                s += Dsp.Sin(Dsp.TwoPi * root * 1.5f * t) * 0.35f;
                d[i] += s * amp * (0.75f + 0.25f * Dsp.Sin(t * 0.4f + p));
            }
        }

        static void Oud(float[] d, Random r, float beat, float rest, int lo, int hi, float amp)
        {
            int deg = (lo + hi) / 2;
            for (float t = 0.4f; t < LoopSeconds; t += beat * (r.NextDouble() < 0.25 ? 0.5f : 1f))
            {
                if (r.NextDouble() < rest) continue;
                deg = Mathf.Clamp(deg + r.Next(-2, 3), lo, hi);
                Dsp.Pluck(d, Rate, t, Note(Hijaz, deg), 1.8f, amp, r, 0.45f);
                if (r.NextDouble() < 0.2) Dsp.Pluck(d, Rate, t + beat * 0.25f, Note(Hijaz, deg + 1), 1f, amp * 0.6f, r, 0.4f);
            }
        }

        static void Heartbeat(float[] d)
        {
            for (float t = 0; t < LoopSeconds; t += 60f / 62f)
            {
                Dsp.Thump(d, Rate, t, 75, 45, 0.5f, 0.55f, 11);
                Dsp.Thump(d, Rate, t + 0.22f, 70, 42, 0.5f, 0.4f, 12);
            }
        }

        static void Shimmers(float[] d, Random r)
        {
            for (float t = 3; t < LoopSeconds; t += Dsp.Rand(r, 6, 9))
            {
                Dsp.Tone(d, Rate, t, Note(Hijaz, 7), 4f, 0.05f, 1.5f, 0.7f);
                Dsp.Tone(d, Rate, t, Note(Hijaz, 8), 4f, 0.05f, 1.5f, 0.7f);
            }
        }

        static void Bells(float[] d, Random r)
        {
            int deg = 9;
            for (float t = 0.5f; t < LoopSeconds; t += Dsp.Rand(r, 1.6f, 2.8f))
            {
                deg = Mathf.Clamp(deg + r.Next(-3, 4), 5, 14);
                Dsp.Bell(d, Rate, t, Note(Hijaz, deg), 3.5f, 0.22f, 1.2f);
            }
        }

        static void Drums(float[] d, Random r)
        {
            float eighth = 60f / 120f / 2f;
            bool[] doum = { true, false, false, true, false, false, false, false };
            bool[] tek = { false, false, true, false, true, false, true, true };
            int step = 0;
            for (float t = 0; t < LoopSeconds; t += eighth, step++)
            {
                int s = step % 8;
                if (doum[s]) { Dsp.Thump(d, Rate, t, 95, 52, 0.6f, 0.75f, 9); Dsp.Burst(d, Rate, t, 0.2f, 0.2f, 400, 20, r); }
                if (tek[s]) { Dsp.Burst(d, Rate, t, 0.08f, 0.35f, 3000, 45, r, true); Dsp.Tone(d, Rate, t, 380, 0.08f, 0.12f, 0.001f, 40); }
            }
        }

        static void Ostinato(float[] d, Random r)
        {
            int[] pattern = { 7, 6, 5, 4, 5, 4, 1, 0 };
            float eighth = 0.25f;
            int i = 0;
            for (float t = 0; t < LoopSeconds; t += eighth, i++)
            {
                if ((i / 16) % 4 == 3 && i % 2 == 1) continue;
                Dsp.Pluck(d, Rate, t, Note(Hijaz, pattern[i % 8] - 7), 0.6f, 0.28f, r, 0.6f);
            }
        }

        static void Pad(float[] d)
        {
            float[] chord = { D3 / 2, D3, D3 * 1.26f, D3 * 1.498f, D3 * 2 };
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate, s = 0;
                foreach (var f in chord) s += Dsp.Sin(Dsp.TwoPi * f * t) + 0.3f * Dsp.Sin(Dsp.TwoPi * f * 2.003f * t);
                d[i] += s * 0.05f * (0.8f + 0.2f * Dsp.Sin(t * 0.5f));
            }
        }

        static void Melody(float[] d, Random r)
        {
            int[] line = { 0, 2, 4, 7, 4, 5, 7, 9, 7, 11, 9, 14 };
            float t = 0.5f;
            for (int rep = 0; rep < 3; rep++)
                foreach (int deg in line)
                {
                    Dsp.Bell(d, Rate, t, Note(Major, deg + 7), 2.5f, 0.16f, 1.6f);
                    Dsp.Pluck(d, Rate, t, Note(Major, deg), 1.4f, 0.22f, r, 0.5f);
                    t += 0.85f;
                    if (t > LoopSeconds) return;
                }
        }
    }
}
