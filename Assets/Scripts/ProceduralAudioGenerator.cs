using System;
using System.IO;
using UnityEngine;

namespace SpellGlide
{
    public static class ProceduralAudioGenerator
    {
        public static AudioClip CreateMenuMusic()
        {
            const int sampleRate = 44100;
            const float duration = 8.0f; // 8 segundos de loop melódico
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];

            // Notas mágicas de fantasia (escala menor natural mística: A3, C4, D4, E4, G4, A4)
            float[] notes = { 220f, 261.63f, 293.66f, 329.63f, 392f, 440f, 523.25f, 659.25f };
            int stepCount = 16;
            int samplesPerStep = totalSamples / stepCount;

            int[] melodyPattern = { 0, 2, 3, 5, 4, 3, 2, 1, 0, 3, 5, 7, 6, 4, 2, 0 };

            for (int step = 0; step < stepCount; step++)
            {
                int noteIndex = melodyPattern[step % melodyPattern.Length];
                float freq = notes[noteIndex % notes.Length];
                int startSample = step * samplesPerStep;

                for (int s = 0; s < samplesPerStep; s++)
                {
                    int index = startSample + s;
                    if (index >= totalSamples) break;

                    float t = (float)s / sampleRate;
                    // Envelope suave com decaimento
                    float envelope = Mathf.Exp(-3.2f * t);

                    // Onda senoidal pura com um harmônico suave para timbre de sino místico
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f
                               + Mathf.Sin(2f * Mathf.PI * (freq * 2f) * t) * 0.25f
                               + Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.15f;

                    // Pad ambiente contínuo de fundo
                    float padFreq = 110f; // A2
                    float padT = (float)index / sampleRate;
                    float ambientPad = (Mathf.Sin(2f * Mathf.PI * padFreq * padT) + Mathf.Sin(2f * Mathf.PI * padFreq * 1.5f * padT)) * 0.08f;

                    data[index] += (wave * envelope * 0.22f) + ambientPad;
                }
            }

            // Normalização suave para evitar clipping
            float max = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                if (Mathf.Abs(data[i]) > max) max = Mathf.Abs(data[i]);
            }
            if (max > 0.0001f)
            {
                for (int i = 0; i < totalSamples; i++) data[i] = (data[i] / max) * 0.7f;
            }

            AudioClip clip = AudioClip.Create("GeneratedMenuMusic", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateClickSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.08f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(900f, 350f, t / duration);
                float env = Mathf.Exp(-35f * t);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("GeneratedClickSound", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateTypewriterSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.045f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];

            var rng = new System.Random();
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-60f * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.35f;
                float tone = Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.65f;
                data[i] = (tone + noise) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create("GeneratedTypewriterSound", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateSkipSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.18f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(300f, 850f, Mathf.Sqrt(t / duration));
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                float harmonic = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.3f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + harmonic) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("GeneratedSkipSound", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreatePlayerFireShotSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.18f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];
            var rng = new System.Random(101);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // 1. Estalo inicial de faísca (snap)
                float sparkSnap = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-60f * t) * 0.7f;

                // 2. Assobio veloz de ar quente
                float flameWhistleFreq = 980f * Mathf.Exp(-12f * t) + 320f;
                float flameWhistle = Mathf.Sin(2f * Mathf.PI * flameWhistleFreq * t) * Mathf.Exp(-16f * t) * 0.55f;

                // 3. Rastro crepitante de brasa
                float sizzle = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Sin(2f * Mathf.PI * flameWhistleFreq * t) * Mathf.Exp(-22f * t) * 0.35f;

                // 4. Punch térmico de saída (mini-poof)
                float punch = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-40f * t) * 0.3f;

                float raw = sparkSnap + flameWhistle + sizzle + punch;
                data[i] = (float)System.Math.Tanh(raw * 1.7f) * 0.9f;
            }

            AudioClip clip = AudioClip.Create("GeneratedPlayerFireShot", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreatePlayerIceShotSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.19f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];
            var rng = new System.Random(202);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // 1. Estalo seco inicial de gelo quebrando
                float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-55f * t) * 0.75f;

                // 2. Assobio veloz de ar congelado
                float whistleFreq = 1800f * (1f - progress * 0.5f);
                float whistle = Mathf.Sin(2f * Mathf.PI * whistleFreq * t) * Mathf.Exp(-15f * t) * 0.5f;

                // 3. Ping cristalino de alta ressonância
                float ping = Mathf.Sin(2f * Mathf.PI * 3400f * t) * Mathf.Exp(-35f * t) * 0.35f;

                data[i] = (crack + whistle + ping) * 0.85f;
            }

            AudioClip clip = AudioClip.Create("GeneratedPlayerIceShot", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateBatFireballSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.17f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];
            var rng = new System.Random(303);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Cuspe/sopro ágil de criatura voadora
                float freq = Mathf.Lerp(780f, 210f, progress);
                float flutter = 1f + 0.35f * Mathf.Sin(2f * Mathf.PI * 45f * t); // Tremolo/vibração alada
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);

                float hissNoise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.55f;
                float env = Mathf.Exp(-18f * t);

                data[i] = (tone * 0.45f + hissNoise * 0.55f) * flutter * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("GeneratedBatFireball", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateBossRockThrowSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.38f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];
            var rng = new System.Random(404);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Projétil pesado e massivo rasgando o ar
                float freq = Mathf.Lerp(70f, 160f, Mathf.Sin(progress * Mathf.PI));
                float sub = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f;
                float sub2 = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.4f;

                // Atrito de cascalho e impacto inicial
                float gravelNoise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.4f;
                float thudEnv = Mathf.Exp(-15f * t) * 0.5f;

                float mainEnv = Mathf.Sin(Mathf.PI * Mathf.Pow(progress, 0.7f));

                float sample = (sub + sub2 + gravelNoise * 0.35f) * (mainEnv * 0.7f + thudEnv);
                data[i] = Mathf.Clamp(sample * 1.3f, -1f, 1f) * 0.8f;
            }

            AudioClip clip = AudioClip.Create("GeneratedBossRockThrow", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateBossStoneWallSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.70f;
            int totalSamples = (int)(sampleRate * duration);
            float[] data = new float[totalSamples];
            var rng = new System.Random(505);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Tremor telúrico / sub-grave sísmico
                float tremorFreq = Mathf.Lerp(45f, 95f, Mathf.SmoothStep(0f, 1f, progress));
                float rumble = Mathf.Sin(2f * Mathf.PI * tremorFreq * t) * 0.6f;
                float rumbleSub = Mathf.Sin(2f * Mathf.PI * (tremorFreq * 0.5f) * t) * 0.45f;

                // Ruído de rochas esmagando e erupção de pedra
                float crunchNoise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float noiseMod = Mathf.Sin(2f * Mathf.PI * 18f * t) * 0.3f + 0.7f;

                // Impacto no meio quando a muralha se firma
                float wallImpact = Mathf.Exp(-18f * Mathf.Abs(t - 0.28f)) * 0.8f;
                float overallEnv = (progress < 0.25f) ? (progress / 0.25f) : Mathf.Exp(-4.5f * (progress - 0.25f));

                float combined = (rumble + rumbleSub + crunchNoise * noiseMod * 0.4f) * overallEnv
                               + (crunchNoise * 0.5f + Mathf.Sin(2f * Mathf.PI * 60f * t)) * wallImpact;

                data[i] = Mathf.Clamp(combined * 1.25f, -1f, 1f) * 0.85f;
            }

            AudioClip clip = AudioClip.Create("GeneratedBossStoneWall", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static byte[] ConvertClipToWav(AudioClip clip)
        {
            int samples = clip.samples;
            int channels = clip.channels;
            int frequency = clip.frequency;

            float[] clipData = new float[samples * channels];
            clip.GetData(clipData, 0);

            short[] intData = new short[clipData.Length];
            byte[] bytesData = new byte[clipData.Length * 2];

            for (int i = 0; i < clipData.Length; i++)
            {
                short val = (short)Mathf.Clamp(clipData[i] * 32767f, -32768f, 32767f);
                byte[] byteArr = BitConverter.GetBytes(val);
                byteArr.CopyTo(bytesData, i * 2);
            }

            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write((int)(36 + bytesData.Length));
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2); // Byte rate
                writer.Write((short)(channels * 2));     // Block align
                writer.Write((short)16);                 // Bits per sample
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(bytesData.Length);
                writer.Write(bytesData);

                return memoryStream.ToArray();
            }
        }
    }
}
