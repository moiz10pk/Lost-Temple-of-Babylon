using System;

namespace Babylon
{
    /// <summary>Tiny DSP toolkit used by ProceduralAudio (deterministic, no allocations per sample).</summary>
    public static class Dsp
    {
        public const float TwoPi = (float)(Math.PI * 2.0);

        public static float Noise(Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
        public static float Rand(Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        /// <summary>One-pole lowpass coefficient for a cutoff in Hz.</summary>
        public static float Coef(float cutoff, int rate) => 1f - (float)Math.Exp(-TwoPi * Math.Max(1f, cutoff) / rate);

        public static float Env(float t, float attack, float decay)
            => t < attack ? t / Math.Max(1e-4f, attack) : (float)Math.Exp(-(t - attack) * decay);

        public static float Hump(float t, float len) { float x = Clamp01(t / len); return (float)Math.Sin(Math.PI * x); }

        public static float Clamp01(float x) => x < 0 ? 0 : (x > 1 ? 1 : x);
        public static float Tanh(float x) => (float)Math.Tanh(x);
        public static float Sin(float phase) => (float)Math.Sin(phase);

        public static void Normalize(float[] d, float peak = 0.9f)
        {
            float m = 1e-6f;
            for (int i = 0; i < d.Length; i++) m = Math.Max(m, Math.Abs(d[i]));
            float g = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= g;
        }

        /// <summary>Lowpass filters an array in place (applied 'passes' times for a steeper slope).</summary>
        public static void Lowpass(float[] d, float cutoff, int rate, int passes = 1)
        {
            float k = Coef(cutoff, rate);
            for (int p = 0; p < passes; p++)
            {
                float z = 0;
                for (int i = 0; i < d.Length; i++) { z += (d[i] - z) * k; d[i] = z; }
            }
        }

        public static void Highpass(float[] d, float cutoff, int rate)
        {
            float k = Coef(cutoff, rate), z = 0;
            for (int i = 0; i < d.Length; i++) { z += (d[i] - z) * k; d[i] -= z; }
        }

        /// <summary>Schroeder-style reverb: 4 combs + 2 allpasses mixed in place.</summary>
        public static void Reverb(float[] d, int rate, float mix, float size = 1f, float feedback = 0.78f)
        {
            int[] combs = { (int)(0.0297f * rate * size), (int)(0.0371f * rate * size), (int)(0.0411f * rate * size), (int)(0.0437f * rate * size) };
            var wet = new float[d.Length];
            foreach (int len in combs)
            {
                var buf = new float[Math.Max(1, len)]; int idx = 0; float lp = 0;
                for (int i = 0; i < d.Length; i++)
                {
                    float y = buf[idx];
                    lp += (y - lp) * 0.45f;
                    buf[idx] = d[i] + lp * feedback;
                    wet[i] += y * 0.25f;
                    idx = (idx + 1) % buf.Length;
                }
            }
            Allpass(wet, (int)(0.005f * rate), 0.7f);
            Allpass(wet, (int)(0.0017f * rate), 0.7f);
            for (int i = 0; i < d.Length; i++) d[i] = d[i] * (1f - mix * 0.5f) + wet[i] * mix;
        }

        static void Allpass(float[] d, int len, float g)
        {
            var buf = new float[Math.Max(1, len)]; int idx = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float b = buf[idx];
                float y = -d[i] * g + b;
                buf[idx] = d[i] + b * g;
                d[i] = y;
                idx = (idx + 1) % buf.Length;
            }
        }

        /// <summary>Crossfades the tail into the head so the clip loops seamlessly. Returns a shorter array.</summary>
        public static float[] MakeLoop(float[] d, int fade)
        {
            fade = Math.Min(fade, d.Length / 3);
            int n = d.Length - fade;
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = d[i];
            for (int i = 0; i < fade; i++)
            {
                float a = (float)i / fade;
                o[i] = d[i] * a + d[n + i] * (1f - a);
            }
            return o;
        }

        /// <summary>Karplus-Strong plucked string (oud-like) mixed into the buffer.</summary>
        public static void Pluck(float[] d, int rate, float start, float freq, float dur, float amp, Random r, float bright = 0.5f)
        {
            int n = Math.Max(2, (int)(rate / freq));
            var buf = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++) { lp += (Noise(r) - lp) * (0.2f + bright * 0.8f); buf[i] = lp; }
            int s0 = (int)(start * rate), len = (int)(dur * rate), idx = 0;
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                if (s0 + i < 0) continue;
                int nx = (idx + 1) % n;
                float y = (buf[idx] + buf[nx]) * 0.5f * 0.996f;
                buf[idx] = y;
                idx = nx;
                float fadeOut = i > len - 400 ? (len - i) / 400f : 1f;
                d[s0 + i] += y * amp * fadeOut;
            }
        }

        /// <summary>FM bell partial mixed into the buffer.</summary>
        public static void Bell(float[] d, int rate, float start, float freq, float dur, float amp, float decay = 2.5f)
        {
            int s0 = (int)(start * rate), len = (int)(dur * rate);
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                if (s0 + i < 0) continue;
                float t = (float)i / rate;
                float env = Env(t, 0.004f, decay);
                float mod = Sin(TwoPi * freq * 1.41f * t) * 2.2f * env;
                d[s0 + i] += Sin(TwoPi * freq * t + mod) * env * amp
                           + Sin(TwoPi * freq * 2.76f * t) * env * env * amp * 0.25f;
            }
        }

        /// <summary>Sine tone with attack/decay envelope.</summary>
        public static void Tone(float[] d, int rate, float start, float freq, float dur, float amp, float attack = 0.01f, float decay = 4f)
        {
            int s0 = (int)(start * rate), len = (int)(dur * rate);
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                if (s0 + i < 0) continue;
                float t = (float)i / rate;
                d[s0 + i] += Sin(TwoPi * freq * t) * Env(t, attack, decay) * amp;
            }
        }

        /// <summary>Pitch-dropping sine thump (drums, booms, footsteps).</summary>
        public static void Thump(float[] d, int rate, float start, float f0, float f1, float dur, float amp, float decay)
        {
            int s0 = (int)(start * rate), len = (int)(dur * rate);
            float ph = 0;
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                if (s0 + i < 0) continue;
                float t = (float)i / rate;
                float f = f1 + (f0 - f1) * (float)Math.Exp(-t * 18f);
                ph += TwoPi * f / rate;
                d[s0 + i] += Sin(ph) * Env(t, 0.002f, decay) * amp;
            }
        }

        /// <summary>Short filtered noise burst.</summary>
        public static void Burst(float[] d, int rate, float start, float dur, float amp, float cutoff, float decay, Random r, bool highpass = false)
        {
            int s0 = (int)(start * rate), len = (int)(dur * rate);
            float k = Coef(cutoff, rate), z = 0;
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                if (s0 + i < 0) continue;
                float t = (float)i / rate;
                float n = Noise(r);
                z += (n - z) * k;
                float v = highpass ? n - z : z;
                d[s0 + i] += v * Env(t, 0.001f, decay) * amp;
            }
        }
    }
}
