using System.Collections.Generic;
using UnityEngine;

namespace GamersWeek
{
    public enum Sfx { Coin, Bounce, Hurt, Stomp, Checkpoint, Click, Door, Alert, Win, Lose }

    // Little retro sound effects made from code, so the kit has sound without any audio files.
    public static class KitSfx
    {
        const int SampleRate = 44100;
        enum Wave { Sine, Square, Noise }

        static readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

        public static void Play(Sfx sfx, float volume = 0.4f)
        {
            var clip = GetClip(sfx);
            var go = new GameObject("Sound: " + sfx) { hideFlags = HideFlags.HideInHierarchy };
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        static AudioClip GetClip(Sfx sfx)
        {
            if (clips.TryGetValue(sfx, out var clip) && clip != null) return clip;
            float[] samples = sfx switch
            {
                Sfx.Coin => Join(Tone(988, 988, 0.07f, Wave.Square), Tone(1319, 1319, 0.22f, Wave.Square)),
                Sfx.Bounce => Tone(180, 720, 0.28f, Wave.Sine),
                Sfx.Hurt => Mix(Tone(220, 90, 0.25f, Wave.Square), Tone(0, 0, 0.25f, Wave.Noise, 0.3f)),
                Sfx.Stomp => Mix(Tone(500, 80, 0.18f, Wave.Square), Tone(0, 0, 0.12f, Wave.Noise, 0.5f)),
                Sfx.Checkpoint => Join(Tone(523, 523, 0.08f, Wave.Square), Tone(659, 659, 0.08f, Wave.Square), Tone(784, 784, 0.2f, Wave.Square)),
                Sfx.Click => Tone(1200, 900, 0.04f, Wave.Square),
                Sfx.Door => Mix(Tone(70, 50, 0.45f, Wave.Square, 0.6f), Tone(0, 0, 0.45f, Wave.Noise, 0.25f)),
                Sfx.Alert => Join(Tone(880, 880, 0.06f, Wave.Square), Tone(1175, 1175, 0.12f, Wave.Square)),
                Sfx.Win => Join(Tone(523, 523, 0.12f, Wave.Square), Tone(659, 659, 0.12f, Wave.Square), Tone(784, 784, 0.12f, Wave.Square), Tone(1047, 1047, 0.5f, Wave.Square)),
                _ => Join(Tone(392, 392, 0.2f, Wave.Square), Tone(330, 330, 0.2f, Wave.Square), Tone(262, 220, 0.6f, Wave.Square)),
            };
            clip = AudioClip.Create(sfx.ToString(), samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            clips[sfx] = clip;
            return clip;
        }

        // One note that slides from one pitch to another and fades out.
        static float[] Tone(float fromHz, float toHz, float seconds, Wave wave, float volume = 1f)
        {
            int count = (int)(seconds * SampleRate);
            var samples = new float[count];
            var random = new System.Random(7);
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                phase += Mathf.Lerp(fromHz, toHz, t) / SampleRate;
                float sine = Mathf.Sin(phase * 2f * Mathf.PI);
                float value = wave switch
                {
                    Wave.Sine => sine,
                    Wave.Square => Mathf.Sign(sine) * 0.5f,
                    _ => (float)(random.NextDouble() * 2.0 - 1.0),
                };
                float fadeIn = Mathf.Min(1f, i / (0.004f * SampleRate));
                samples[i] = value * fadeIn * (1f - t) * volume;
            }
            return samples;
        }

        static float[] Join(params float[][] parts)
        {
            var result = new List<float>();
            foreach (var part in parts) result.AddRange(part);
            return result.ToArray();
        }

        static float[] Mix(float[] a, float[] b)
        {
            var result = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < result.Length; i++)
                result[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
            return result;
        }
    }
}
