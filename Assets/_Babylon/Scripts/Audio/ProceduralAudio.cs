using System;
using UnityEngine;
using Random = System.Random;

namespace Babylon
{
    /// <summary>Synthesizes every sound effect from code. Used by the editor (writes WAVs) and as a runtime fallback.</summary>
    public static class ProceduralAudio
    {
        public const int Rate = 44100;

        public static bool IsLoop(SoundId id) =>
            id == SoundId.DesertWind || id == SoundId.TorchCrackle || id == SoundId.SpiderSkitter ||
            id == SoundId.Rumble || id == SoundId.CaveDrips;

        public static AudioClip CreateClip(SoundId id)
        {
            var data = Render(id);
            var clip = AudioClip.Create(id.ToString(), data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] Render(SoundId id)
        {
            var r = new Random((int)id * 7919 + 17);
            float[] d;
            switch (id)
            {
                case SoundId.DesertWind: d = Wind(r); break;
                case SoundId.TorchCrackle: d = Crackle(r); break;
                case SoundId.Footstep: d = Step(r, 0.22f, 1100f, 85f, 0.55f); break;
                case SoundId.Land: d = Step(r, 0.32f, 700f, 60f, 1f); break;
                case SoundId.SwordWhoosh: d = Whoosh(r, 0.4f, 500f, 2800f); break;
                case SoundId.DartWhoosh: d = Whoosh(r, 0.3f, 1500f, 5000f); break;
                case SoundId.Throw: d = Whoosh(r, 0.6f, 200f, 900f); break;
                case SoundId.Roll: d = RollSound(r); break;
                case SoundId.HitThud: d = Buf(0.35f); Dsp.Thump(d, Rate, 0, 120, 45, 0.35f, 1f, 13f); Dsp.Burst(d, Rate, 0, 0.1f, 0.6f, 1500, 30, r); break;
                case SoundId.StoneClang: d = Clang(r); break;
                case SoundId.SpiderHiss: d = Hiss(r); break;
                case SoundId.SpiderSkitter: d = Skitter(r); break;
                case SoundId.KeyJingle: d = Jingle(r); break;
                case SoundId.HealShimmer: d = Shimmer(r); break;
                case SoundId.Chime: d = Buf(2.6f); Dsp.Bell(d, Rate, 0, 880, 2.5f, 0.6f, 1.8f); Dsp.Bell(d, Rate, 0.06f, 1318.5f, 2.4f, 0.4f, 2.0f); Dsp.Reverb(d, Rate, 0.35f); break;
                case SoundId.WrongBuzz: d = Buzz(); break;
                case SoundId.DoorGrind: d = Grind(r); break;
                case SoundId.BossRoar: d = Roar(r); break;
                case SoundId.GroundSmash: d = Smash(r); break;
                case SoundId.Crumble: d = CrumbleSound(r); break;
                case SoundId.VictoryFanfare: d = Fanfare(); break;
                case SoundId.UIClick: d = Buf(0.07f); Dsp.Tone(d, Rate, 0, 1800, 0.07f, 0.5f, 0.001f, 70f); Dsp.Burst(d, Rate, 0, 0.01f, 0.3f, 6000, 200, r, true); break;
                case SoundId.ScorpionSting: d = Buf(0.6f); Dsp.Burst(d, Rate, 0, 0.05f, 0.9f, 3000, 60, r, true); Dsp.Thump(d, Rate, 0.03f, 260, 80, 0.4f, 0.9f, 20); Dsp.Burst(d, Rate, 0.05f, 0.5f, 0.25f, 4000, 6, r, true); break;
                case SoundId.PlayerHurt: d = Hurt(r); break;
                case SoundId.BossStep: d = Buf(0.9f); Dsp.Thump(d, Rate, 0, 55, 28, 0.9f, 1f, 6f); Dsp.Burst(d, Rate, 0, 0.5f, 0.6f, 260, 9, r); Dsp.Reverb(d, Rate, 0.25f, 1.6f); break;
                case SoundId.Rumble: d = RumbleLoop(r); break;
                case SoundId.SwordShing: d = Shing(r); break;
                case SoundId.CaveDrips: d = Drips(r); break;
                default: d = Buf(0.05f); break;
            }
            for (int i = 0; i < d.Length; i++) d[i] = Dsp.Tanh(d[i] * 1.1f);
            Dsp.Normalize(d, IsLoop(id) ? 0.7f : 0.9f);
            return d;
        }

        static float[] Buf(float sec) => new float[(int)(sec * Rate)];

        static float[] Wind(Random r)
        {
            var d = Buf(11f);
            float lp1 = 0, lp2 = 0, p1 = Dsp.Rand(r, 0, 6), p2 = Dsp.Rand(r, 0, 6);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float gust = 0.5f + 0.3f * Dsp.Sin(t * 0.55f + p1) + 0.2f * Dsp.Sin(t * 1.37f + p2);
                float k = Dsp.Coef(250f + 1100f * gust * gust, Rate);
                lp1 += (Dsp.Noise(r) - lp1) * k; lp2 += (lp1 - lp2) * k;
                d[i] = lp2 * (0.25f + 0.75f * gust);
            }
            return Dsp.MakeLoop(d, Rate * 2);
        }

        static float[] Crackle(Random r)
        {
            var d = Buf(4.6f);
            float lp = 0;
            for (int i = 0; i < d.Length; i++) { lp += (Dsp.Noise(r) - lp) * 0.02f; d[i] = lp * 0.6f; }
            for (float t = 0; t < 4.5f; t += Dsp.Rand(r, 0.01f, 0.12f))
            {
                float a = (float)Math.Pow(r.NextDouble(), 3) * 0.9f;
                Dsp.Burst(d, Rate, t, 0.015f, a, 2500, 300, r, true);
            }
            return Dsp.MakeLoop(d, Rate / 2);
        }

        static float[] Step(Random r, float len, float cutoff, float thump, float thumpAmp)
        {
            var d = Buf(len);
            Dsp.Burst(d, Rate, 0, len, 1f, cutoff, 32, r);
            Dsp.Thump(d, Rate, 0, thump * 1.6f, thump, len, thumpAmp, 30);
            Dsp.Burst(d, Rate, 0.005f, 0.05f, 0.15f, 5000, 60, r, true);
            return d;
        }

        static float[] Whoosh(Random r, float len, float f0, float f1)
        {
            var d = Buf(len);
            float a = 0, b = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate, h = Dsp.Hump(t, len);
                float n = Dsp.Noise(r);
                a += (n - a) * Dsp.Coef(f0 + (f1 - f0) * h, Rate);
                b += (n - b) * Dsp.Coef(f0 * 0.5f, Rate);
                d[i] = (a - b) * h * h;
            }
            return d;
        }

        static float[] RollSound(Random r)
        {
            var d = Whoosh(r, 0.5f, 200f, 900f);
            Dsp.Thump(d, Rate, 0.32f, 90, 50, 0.18f, 0.6f, 25);
            return d;
        }

        static float[] Clang(Random r)
        {
            var d = Buf(1.1f);
            float[,] p = { { 430, 1, 6 }, { 1190, 0.7f, 9 }, { 1730, 0.5f, 11 }, { 2610, 0.35f, 14 }, { 3480, 0.2f, 18 } };
            for (int k = 0; k < 5; k++) Dsp.Tone(d, Rate, 0, p[k, 0] * Dsp.Rand(r, 0.98f, 1.02f), 1.1f, p[k, 1] * 0.5f, 0.001f, p[k, 2]);
            Dsp.Burst(d, Rate, 0, 0.2f, 0.8f, 1800, 25, r);
            Dsp.Reverb(d, Rate, 0.2f);
            return d;
        }

        static float[] Hiss(Random r)
        {
            var d = Buf(1.1f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float env = t < 0.08f ? t / 0.08f : (float)Math.Exp(-(t - 0.08f) * 3.2f);
                d[i] = Dsp.Noise(r) * env * (0.75f + 0.25f * Dsp.Sin(Dsp.TwoPi * 31 * t));
            }
            Dsp.Highpass(d, 2600, Rate);
            Dsp.Lowpass(d, 9000, Rate);
            return d;
        }

        static float[] Skitter(Random r)
        {
            var d = Buf(1.7f);
            for (float t = 0; t < 1.65f; t += Dsp.Rand(r, 0.022f, 0.06f))
                Dsp.Burst(d, Rate, t, 0.012f, Dsp.Rand(r, 0.3f, 1f), 3500, 380, r, true);
            return Dsp.MakeLoop(d, Rate / 20);
        }

        static float[] Jingle(Random r)
        {
            var d = Buf(1f);
            for (int k = 0; k < 8; k++)
            {
                float t = Dsp.Rand(r, 0, 0.4f), f = Dsp.Rand(r, 2400, 5200);
                Dsp.Tone(d, Rate, t, f, 0.6f, 0.3f, 0.001f, 12); Dsp.Tone(d, Rate, t, f * 2.76f, 0.4f, 0.12f, 0.001f, 18);
            }
            return d;
        }

        static float[] Shimmer(Random r)
        {
            var d = Buf(1.8f);
            float[] f = { 587.3f, 740f, 880f, 1174.7f, 1480f };
            for (int k = 0; k < f.Length; k++)
            {
                Dsp.Tone(d, Rate, k * 0.11f, f[k], 1.4f, 0.3f, 0.02f, 3);
                Dsp.Tone(d, Rate, k * 0.11f, f[k] * 1.004f, 1.4f, 0.2f, 0.02f, 3);
            }
            Dsp.Burst(d, Rate, 0, 1.2f, 0.08f, 7000, 2, r, true);
            Dsp.Reverb(d, Rate, 0.35f);
            return d;
        }

        static float[] Buzz()
        {
            var d = Buf(0.75f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float s = ((t * 98f) % 1f) * 2 - 1 + ((t * 104f) % 1f) * 2 - 1;
                float env = t < 0.01f ? t / 0.01f : (t < 0.45f ? 1f : (float)Math.Exp(-(t - 0.45f) * 14f));
                d[i] = s * env * 0.5f;
            }
            Dsp.Lowpass(d, 900, Rate, 2);
            return d;
        }

        static float[] Grind(Random r)
        {
            var d = Buf(3f);
            float am = 0.5f, target = 0.5f, lp = 0, lp2 = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                if (i % 1200 == 0) target = Dsp.Rand(r, 0.3f, 1f);
                am += (target - am) * 0.002f;
                float n = Dsp.Noise(r);
                lp += (n - lp) * 0.05f; lp2 += (lp - lp2) * 0.05f;
                float env = Dsp.Clamp01(t / 0.25f) * Dsp.Clamp01((3f - t) / 0.6f);
                d[i] = (lp2 * 2.5f * am + (n - lp) * 0.04f * am + Dsp.Sin(Dsp.TwoPi * 42 * t) * 0.25f) * env;
            }
            Dsp.Reverb(d, Rate, 0.2f, 1.4f);
            return d;
        }

        static float[] Roar(Random r)
        {
            var d = Buf(2.8f);
            float ph1 = 0, ph2 = 0, lp = 0, br = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 48f + 50f * (float)Math.Exp(-t * 1.1f);
                f *= 1f + 0.03f * Dsp.Sin(Dsp.TwoPi * 6.5f * t);
                ph1 += f / Rate; ph2 += f * 1.498f / Rate;
                float saw = (ph1 % 1f) * 2 - 1 + ((ph2 % 1f) * 2 - 1) * 0.6f;
                lp += (saw - lp) * Dsp.Coef(1300f - 800f * Dsp.Clamp01(t / 2.2f), Rate);
                br += (Dsp.Noise(r) - br) * 0.08f;
                float env = Dsp.Clamp01(t / 0.18f) * Dsp.Clamp01((2.6f - t) / 0.7f);
                d[i] = Dsp.Tanh((lp + br * 0.6f) * 2.5f) * env;
            }
            Dsp.Reverb(d, Rate, 0.4f, 1.8f, 0.82f);
            return d;
        }

        static float[] Smash(Random r)
        {
            var d = Buf(2.4f);
            Dsp.Thump(d, Rate, 0, 90, 26, 2.4f, 1f, 2.4f);
            Dsp.Burst(d, Rate, 0, 1.5f, 0.9f, 600, 5, r);
            Dsp.Burst(d, Rate, 0, 0.15f, 0.6f, 3000, 25, r, true);
            for (int i = 0; i < d.Length; i++) d[i] = Dsp.Tanh(d[i] * 1.8f);
            Dsp.Reverb(d, Rate, 0.35f, 1.8f, 0.8f);
            return d;
        }

        static float[] CrumbleSound(Random r)
        {
            var d = Buf(3.4f);
            for (int k = 0; k < 70; k++)
            {
                float t = 2.9f * (float)Math.Pow(r.NextDouble(), 1.6);
                Dsp.Burst(d, Rate, t, 0.12f, Dsp.Rand(r, 0.2f, 1f) * (1f - t / 3.4f), Dsp.Rand(r, 300, 2200), 35, r);
                if (r.NextDouble() < 0.3) Dsp.Thump(d, Rate, t, 140, 60, 0.2f, 0.4f, 20);
            }
            Dsp.Burst(d, Rate, 0, 3.2f, 0.5f, 120, 1.2f, r);
            Dsp.Reverb(d, Rate, 0.3f, 1.6f);
            return d;
        }

        static void Brass(float[] d, float start, float freq, float dur, float amp)
        {
            int s0 = (int)(start * Rate), len = (int)(dur * Rate);
            float ph = 0, lp = 0;
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                ph += freq * (1f + 0.004f * Dsp.Sin(Dsp.TwoPi * 5 * t)) / Rate;
                float saw = (ph % 1f) * 2 - 1;
                float env = Dsp.Clamp01(t / 0.04f) * Dsp.Clamp01((dur - t) / 0.25f);
                lp += (saw - lp) * Dsp.Coef(600 + 2400 * (float)Math.Exp(-t * 3), Rate);
                d[s0 + i] += lp * env * amp;
            }
        }

        static float[] Fanfare()
        {
            var d = Buf(4.4f);
            float[] seq = { 293.7f, 440f, 587.3f, 740f };
            for (int k = 0; k < 4; k++) Brass(d, k * 0.22f, seq[k], 0.3f, 0.35f);
            float[] chord = { 293.7f, 370f, 440f, 587.3f, 880f };
            foreach (var f in chord) Brass(d, 0.95f, f, 2.8f, 0.22f);
            Dsp.Thump(d, Rate, 0, 110, 70, 1f, 0.7f, 4); Dsp.Thump(d, Rate, 0.95f, 110, 70, 1.5f, 0.9f, 3);
            Dsp.Bell(d, Rate, 0.95f, 1174.7f, 3f, 0.15f, 1.5f);
            Dsp.Reverb(d, Rate, 0.35f, 1.5f);
            return d;
        }

        static float[] Hurt(Random r)
        {
            var d = Buf(0.45f);
            float ph = 0, a = 0, b = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                ph += (160f - 60f * t / 0.45f) / Rate;
                float s = (ph % 1f) * 2 - 1 + Dsp.Noise(r) * 0.3f;
                a += (s - a) * Dsp.Coef(900, Rate); b += (a - b) * Dsp.Coef(350, Rate);
                d[i] = (a - b) * Dsp.Env(t, 0.02f, 7f);
            }
            return d;
        }

        static float[] RumbleLoop(Random r)
        {
            var d = Buf(3.6f);
            for (int i = 0; i < d.Length; i++) d[i] = Dsp.Noise(r);
            Dsp.Lowpass(d, 90, Rate, 3);
            for (int i = 0; i < d.Length; i++) d[i] *= 0.7f + 0.3f * Dsp.Sin(i * 0.0004f);
            return Dsp.MakeLoop(d, Rate / 2);
        }

        static float[] Shing(Random r)
        {
            var d = Buf(1.4f);
            for (int i = 0; i < (int)(0.18f * Rate); i++) d[i] = Dsp.Noise(r) * (i / (0.18f * Rate)) * 0.4f;
            Dsp.Highpass(d, 3000, Rate);
            float[] f = { 2210, 3320, 5080, 6710 };
            foreach (var x in f) Dsp.Tone(d, Rate, 0.16f, x, 1.2f, 0.25f, 0.002f, 3.5f);
            Dsp.Reverb(d, Rate, 0.3f);
            return d;
        }

        static float[] Drips(Random r)
        {
            var d = Buf(9f);
            for (float t = 0.3f; t < 8.5f; t += Dsp.Rand(r, 0.7f, 2.2f))
            {
                int s0 = (int)(t * Rate); float ph = 0, f0 = Dsp.Rand(r, 700, 1300);
                for (int i = 0; i < Rate / 8 && s0 + i < d.Length; i++)
                {
                    float tt = (float)i / Rate;
                    ph += f0 * (1f + 1.5f * Dsp.Clamp01(tt / 0.015f)) / Rate;
                    d[s0 + i] += Dsp.Sin(Dsp.TwoPi * ph) * (float)Math.Exp(-tt * 45) * 0.5f;
                }
            }
            Dsp.Reverb(d, Rate, 0.55f, 2.2f, 0.85f);
            return Dsp.MakeLoop(d, Rate);
        }
    }
}
