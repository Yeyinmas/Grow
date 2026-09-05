using System.Collections.Generic;
using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 全局音效播放器：从 Resources/Sounds/ 按 key 加载音效，用 PlayOneShot 播放。
    /// 首次播放时自动创建一个跨场景持久的 AudioSource，无需在场景里手动摆放任何物体。
    /// </summary>
    public static class AudioManager
    {
        static AudioSource _source;
        static AudioSource _musicSource;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        // 每次进入 Play 时清空缓存，兼容「关闭 Reload Domain」的情况。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            Cache.Clear();
            _source = null;
            _musicSource = null;
        }

        static AudioSource Source
        {
            get
            {
                if (_source == null)
                {
                    var go = new GameObject("AudioManager");
                    Object.DontDestroyOnLoad(go);
                    _source = go.AddComponent<AudioSource>();
                    _source.playOnAwake = false;
                }
                return _source;
            }
        }

        /// <summary>播放指定 key 的音效（Assets/Resources/Sounds/{key}）。找不到则静默忽略。</summary>
        public static void Play(string key)
        {
            var clip = GetClip(key);
            if (clip == null) return;
            Source.PlayOneShot(clip);
        }

        static AudioClip GetClip(string key)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var clip = Resources.Load<AudioClip>("Sounds/" + key);
            if (clip != null) Cache[key] = clip;
            return clip;
        }

        // ---------- 背景音乐 ----------

        static AudioSource MusicSource
        {
            get
            {
                if (_musicSource == null)
                {
                    var go = new GameObject("AudioManagerMusic");
                    Object.DontDestroyOnLoad(go);
                    _musicSource = go.AddComponent<AudioSource>();
                    _musicSource.playOnAwake = false;
                    _musicSource.loop = true;      // 循环播放
                    _musicSource.spatialBlend = 0f; // 2D，不受位置/距离影响
                }
                return _musicSource;
            }
        }

        /// <summary>
        /// 循环播放背景音乐（Assets/Resources/Music/{key}）。
        /// 挂在一个 DontDestroyOnLoad 物体上，切换场景不会打断、也不会重置播放进度。
        /// 若同一首音乐已在播放则直接返回（不重播）。
        /// </summary>
        public static void PlayMusic(string key)
        {
            var clip = GetMusicClip(key);
            if (clip == null) return;

            var src = MusicSource;
            if (src.clip == clip && src.isPlaying) return; // 已在播同一首，保持进度
            src.clip = clip;
            src.Play();
        }

        static AudioClip GetMusicClip(string key)
        {
            if (Cache.TryGetValue("Music/" + key, out var cached) && cached != null) return cached;
            var clip = Resources.Load<AudioClip>("Music/" + key);
            if (clip != null) Cache["Music/" + key] = clip;
            return clip;
        }
    }
}
