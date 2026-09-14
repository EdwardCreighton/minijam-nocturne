using System.Collections;
using System.Collections.Generic;
using Nocturne.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nocturne.Tests.PlayMode
{
    /// <summary>
    /// Плейлист уровня: старт после DismissBriefing-логики, per-track громкость
    /// (effective = global * track), автопереход на следующий, пауза с миром.
    /// Клипы короткие синтезированные; ожидания только unscaled/realtime.
    /// </summary>
    public sealed class LevelMusicPlaylistTests
    {
        private GameObject audioGo;
        private AudioManager audio;
        private readonly List<AudioClip> clips = new();

        [TearDown]
        public void TearDown()
        {
            AudioListener.pause = false;
            foreach (var c in clips)
                if (c != null) Object.DestroyImmediate(c);
            clips.Clear();
            if (audioGo != null) Object.DestroyImmediate(audioGo);
            audioGo = null;
            audio = null;
        }

        [UnityTest]
        public IEnumerator Playlist_Starts_WithEffectiveVolume_AndAdvances()
        {
            SetupPlaylist(
                (MakeClip("pl_a", 1f), 1f),
                (MakeClip("pl_b", 1f), 0.5f));

            audio.PlayLevelPlaylist();
            yield return null;

            Assert.IsTrue(audio.IsPlaylistActive);
            Assert.IsNotNull(audio.CurrentMusicClip);

            // effective = global * track
            var first = audio.CurrentMusicClip;
            var expected = audio.GlobalMusicVolume * (first == clips[0] ? 1f : 0.5f);
            Assert.AreEqual(expected, audio.CurrentMusicVolume, 0.001f);

            // Автопереход: ждем смену клипа (клипы по 1с, таймаут с запасом).
            var elapsed = 0f;
            while (audio.CurrentMusicClip == first && elapsed < 8f)
            {
                yield return new WaitForSecondsRealtime(0.2f);
                elapsed += 0.2f;
            }
            Assert.AreNotEqual(first, audio.CurrentMusicClip, "playlist did not advance to the next track");
        }

        [UnityTest]
        public IEnumerator Playlist_Pauses_WithWorld_AndResumes()
        {
            SetupPlaylist(
                (MakeClip("pl_c", 1f), 0.8f),
                (MakeClip("pl_d", 1f), 0.8f));

            audio.PlayLevelPlaylist();
            yield return null;
            var first = audio.CurrentMusicClip;

            try
            {
                // Морозим мир сразу: клип (1с) не должен смениться за 2с паузы.
                AudioListener.pause = true;
                yield return new WaitForSecondsRealtime(2f);
                Assert.AreEqual(first, audio.CurrentMusicClip, "track advanced while paused");
                Assert.IsTrue(audio.IsPlaylistActive);

                // Разморозка — очередь продолжается.
                AudioListener.pause = false;
                var elapsed = 0f;
                while (audio.CurrentMusicClip == first && elapsed < 8f)
                {
                    yield return new WaitForSecondsRealtime(0.2f);
                    elapsed += 0.2f;
                }
                Assert.AreNotEqual(first, audio.CurrentMusicClip, "playlist did not resume after unpause");
            }
            finally
            {
                AudioListener.pause = false;
            }
        }

        [UnityTest]
        public IEnumerator Playlist_Empty_StaysSilent()
        {
            audioGo = new GameObject("AudioPlaylistTest");
            audio = audioGo.AddComponent<AudioManager>();
            yield return null;

            audio.PlayLevelPlaylist();
            yield return null;

            Assert.IsFalse(audio.IsPlaylistActive);
            Assert.IsNull(audio.CurrentMusicClip);
        }

        private void SetupPlaylist(params (AudioClip clip, float volume)[] entries)
        {
            audioGo = new GameObject("AudioPlaylistTest");
            audio = audioGo.AddComponent<AudioManager>();
            var tracks = new List<AudioManager.LevelMusicTrack>();
            foreach (var (clip, volume) in entries)
                tracks.Add(new AudioManager.LevelMusicTrack { clip = clip, volume = volume });
            audio.OverridePlaylist(tracks);
        }

        private AudioClip MakeClip(string name, float seconds)
        {
            const int freq = 4410;
            var c = AudioClip.Create(name, Mathf.Max(1, Mathf.RoundToInt(freq * seconds)), 1, freq, false);
            clips.Add(c);
            return c;
        }
    }
}
