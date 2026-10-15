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
