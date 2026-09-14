using System.Collections.Generic;
using Nocturne.Core;
using NUnit.Framework;
using UnityEngine;

namespace Nocturne.Tests.EditMode
{
    /// <summary>
    /// Чистая логика очереди плейлиста: покрытие без повторов, детерминизм,
    /// фильтрация битых слотов. Без реального звука.
    /// </summary>
    public sealed class LevelPlaylistTests
    {
        [Test]
        public void Queue_CoversAllIndices_WithoutRepeats()
        {
            var q = AudioManager.BuildShuffledQueue(new List<int> { 0, 1, 2, 3 }, seed: 42);
            Assert.AreEqual(4, q.Count);
            Assert.AreEqual(new HashSet<int> { 0, 1, 2, 3 }, new HashSet<int>(q));
        }

        [Test]
        public void Queue_IsDeterministic_OnSameSeed()
        {
            var a = AudioManager.BuildShuffledQueue(new List<int> { 0, 1, 2, 3, 4 }, seed: 7);
            var b = AudioManager.BuildShuffledQueue(new List<int> { 0, 1, 2, 3, 4 }, seed: 7);
            Assert.AreEqual(a, b);
        }

        [Test]
        public void Queue_EmptyAndNull_ReturnsEmpty()
        {
            Assert.IsEmpty(AudioManager.BuildShuffledQueue(new List<int>(), seed: 1));
            Assert.IsEmpty(AudioManager.BuildShuffledQueue(null, seed: 1));
        }

        [Test]
        public void QueueForTracks_SkipsNullEntries_AndKeepsValid()
        {
            var good = AudioClip.Create("pl_good", 4410, 1, 4410, false);
            try
            {
                var tracks = new List<AudioManager.LevelMusicTrack>
                {
                    new() { clip = null, volume = 0.5f },
                    null,
                    new() { clip = good, volume = 0.5f },
                };
                var q = AudioManager.BuildQueueForTracks(tracks, seed: 3);
                Assert.AreEqual(new List<int> { 2 }, q);
            }
            finally
            {
                Object.DestroyImmediate(good);
            }
        }

        [Test]
        public void QueueForTracks_EmptyOrNull_ReturnsEmpty()
        {
            Assert.IsEmpty(AudioManager.BuildQueueForTracks(new List<AudioManager.LevelMusicTrack>(), seed: 1));
            Assert.IsEmpty(AudioManager.BuildQueueForTracks(null, seed: 1));
        }
    }
}
