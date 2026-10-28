using System.Collections.Generic;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>Sonidos y música sintetizados al arrancar (no hay archivos de audio).</summary>
    public sealed class Sfx
    {
        private const int Rate = 22050;

        private readonly AudioSource[] _sources;
        private readonly AudioSource _music;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private int _next;

        public bool SoundOn = true;

        /// <summary>Silencia los efectos (la demo de fondo del menú no hace ruido).</summary>
        public bool Muted;

        public Sfx(GameObject host)
        {
            _sources = new AudioSource[6];
            for (int i = 0; i < _sources.Length; i++)
            {
                _sources[i] = host.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
            }
            _music = host.AddComponent<AudioSource>();
            _music.loop = true;
            _music.volume = 0.32f;
            _music.playOnAwake = false;
            Build();
        }

        public void Play(string name, float volume = 1f, float pitch = 1f)
        {
            if (!SoundOn || Muted || !_clips.TryGetValue(name, out var clip)) return;
            var s = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume);
        }

        public void SetMusic(bool on)
        {
            if (on && !_music.isPlaying) _music.Play();
            else if (!on && _music.isPlaying) _music.Stop();
        }

        // ------------------------------------------------------------------ síntesis

        private enum Wave { Sine, Square, Triangle, Saw, Noise }

        private static float Osc(Wave w, float phase, System.Random rng)
        {
            float f = phase - Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Square: return f < 0.5f ? 0.6f : -0.6f;
                case Wave.Triangle: return 4f * Mathf.Abs(f - 0.5f) - 1f;
                case Wave.Saw: return (2f * f - 1f) * 0.6f;
                case Wave.Noise: return (float)rng.NextDouble() * 2f - 1f;
                default: return Mathf.Sin(phase * Mathf.PI * 2f);
            }
        }

        /// <summary>Suma una nota con barrido de frecuencia y envolvente simple a un buffer.</summary>
        private static void Note(float[] buf, float start, float dur, float f0, float f1, Wave w, float vol,
            float attack = 0.005f, float vibrato = 0f, System.Random rng = null)
        {
            rng ??= new System.Random(7);
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            float phase = 0f;
            float lowpass = 0f;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                float t = i / (float)Rate;
                float k = i / (float)n;
                float f = Mathf.Lerp(f0, f1, k) * (1f + vibrato * Mathf.Sin(t * 40f));
                phase += f / Rate;
                float env = Mathf.Min(1f, t / attack) * (1f - k) * (1f - k);
                float v = Osc(w, phase, rng);
                if (w == Wave.Noise) { lowpass += (v - lowpass) * Mathf.Clamp01(f / 4000f); v = lowpass; }
                buf[s0 + i] += v * env * vol;
            }
        }

        private void Add(string name, float seconds, System.Action<float[]> build)
        {
            var buf = new float[(int)(seconds * Rate)];
            build(buf);
            for (int i = 0; i < buf.Length; i++) buf[i] = Mathf.Clamp(buf[i], -1f, 1f);
            var clip = AudioClip.Create(name, buf.Length, 1, Rate, false);
            clip.SetData(buf, 0);
            _clips[name] = clip;
        }

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        private void Build()
        {
            Add("pickup", 0.3f, b => { Note(b, 0, 0.12f, 660, 700, Wave.Triangle, 0.6f); Note(b, 0.09f, 0.2f, 990, 1040, Wave.Triangle, 0.6f); });
            Add("steal", 0.4f, b => { Note(b, 0, 0.14f, 2500, 800, Wave.Noise, 0.7f); Note(b, 0, 0.3f, 700, 160, Wave.Square, 0.35f); });
            Add("flip", 0.25f, b => Note(b, 0, 0.22f, 280, 820, Wave.Sine, 0.55f, 0.02f));
            Add("stick", 0.2f, b => { Note(b, 0, 0.16f, 420, 240, Wave.Sine, 0.5f); Note(b, 0, 0.03f, 3000, 2000, Wave.Noise, 0.3f); });
            Add("jump", 0.12f, b => Note(b, 0, 0.1f, 300, 540, Wave.Square, 0.25f));
            Add("land", 0.08f, b => Note(b, 0, 0.07f, 900, 300, Wave.Noise, 0.35f));
            Add("pipein", 0.35f, b => Note(b, 0, 0.32f, 520, 110, Wave.Sine, 0.6f, 0.005f, 0.06f));
            Add("pipeout", 0.25f, b => Note(b, 0, 0.22f, 130, 640, Wave.Sine, 0.6f, 0.005f, 0.05f));
            Add("tick", 0.06f, b => Note(b, 0, 0.05f, 1250, 1250, Wave.Square, 0.25f));
            Add("count", 0.2f, b => Note(b, 0, 0.18f, 523, 523, Wave.Square, 0.3f));
            Add("go", 0.45f, b => { Note(b, 0, 0.4f, 784, 784, Wave.Square, 0.25f); Note(b, 0, 0.4f, 1047, 1047, Wave.Triangle, 0.35f); });
            Add("bump", 0.14f, b => Note(b, 0, 0.12f, 140, 70, Wave.Sine, 0.7f));
            Add("ui", 0.07f, b => Note(b, 0, 0.06f, 720, 760, Wave.Triangle, 0.4f));
            Add("round", 0.8f, b =>
            {
                int[] notes = { 72, 76, 79, 84 };
                for (int i = 0; i < notes.Length; i++) Note(b, i * 0.1f, 0.35f, Midi(notes[i]), Midi(notes[i]), Wave.Triangle, 0.45f);
            });
            Add("win", 1.6f, b =>
            {
                int[] notes = { 67, 72, 76, 79, 76, 79, 84 };
                float[] at = { 0f, 0.12f, 0.24f, 0.36f, 0.6f, 0.72f, 0.84f };
                for (int i = 0; i < notes.Length; i++)
                {
                    float d = i == notes.Length - 1 ? 0.7f : 0.2f;
                    Note(b, at[i], d, Midi(notes[i]), Midi(notes[i]), Wave.Square, 0.22f);
                    Note(b, at[i], d, Midi(notes[i] - 12), Midi(notes[i] - 12), Wave.Triangle, 0.3f);
                }
            });

            // Música: cuatro compases que se repiten (La menor, Fa, Do, Sol) con bajo, arpegio y platillos.
            const float bpm = 112f;
            float beat = 60f / bpm;
            float total = beat * 16f;
            var music = new float[(int)(total * Rate)];
            var rng = new System.Random(3);
            int[][] chords = { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 55, 59, 62 } };
            for (int bar = 0; bar < 4; bar++)
            {
                var ch = chords[bar];
                float t0 = bar * beat * 4f;
                for (int q = 0; q < 4; q++)
                {
                    int root = ch[0] - 12;
                    Note(music, t0 + q * beat, beat * 0.9f, Midi(root), Midi(root), Wave.Triangle, 0.45f, 0.01f);
                    if (q % 2 == 1) Note(music, t0 + q * beat + beat * 0.5f, beat * 0.4f, Midi(root + 7), Midi(root + 7), Wave.Triangle, 0.3f, 0.01f);
                }
                for (int e = 0; e < 8; e++)
                {
                    int n = ch[e % 3] + (e >= 4 ? 12 : 0);
                    Note(music, t0 + e * beat * 0.5f, beat * 0.45f, Midi(n), Midi(n), Wave.Square, 0.07f, 0.004f);
                    Note(music, t0 + e * beat * 0.5f + beat * 0.25f, 0.04f, 7000, 6000, Wave.Noise, e % 2 == 1 ? 0.12f : 0.06f, 0.001f, 0f, rng);
                }
                Note(music, t0, 0.12f, 120, 50, Wave.Sine, 0.5f);
                Note(music, t0 + beat * 2f, 0.12f, 120, 50, Wave.Sine, 0.5f);
                Note(music, t0 + beat, 0.1f, 1800, 900, Wave.Noise, 0.25f, 0.001f, 0f, rng);
                Note(music, t0 + beat * 3f, 0.1f, 1800, 900, Wave.Noise, 0.25f, 0.001f, 0f, rng);
            }
            for (int i = 0; i < music.Length; i++) music[i] = Mathf.Clamp(music[i] * 0.8f, -1f, 1f);
            var clip = AudioClip.Create("musica", music.Length, 1, Rate, false);
            clip.SetData(music, 0);
            _music.clip = clip;
        }
    }
}
