using System.Collections.Generic;
using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 贴图加载器。优先从 Resources/Sprites/ 下按名字加载图片，
    /// 找不到时回退到程序生成的纯色方块，方便在没有美术素材时也能运行。
    ///
    /// 约定：把素材图片放到 Assets/Resources/Sprites/ 文件夹内，
    /// 文件名（不含扩展名）即 key。例如 player.png 对应 Get("player", ...)。
    /// </summary>
    public static class SpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // 每次进入 Play 时清空缓存。
        // 这是为了兼容「Enter Play Mode Options 里关闭 Reload Domain」的情况：
        // 否则 static 缓存会残留上一次运行已销毁的 Sprite（其贴图已失效），
        // 命中缓存后返回僵尸贴图，导致物体看起来透明/不可见，且每隔一次运行才复现。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => Cache.Clear();

        public static Sprite Get(string name, Color fallbackColor)
        {
            // 命中缓存时再判一次 null：若缓存的 Sprite 已被 Unity 销毁（== null），
            // 就当作未命中，重新创建，避免返回僵尸贴图。
            if (Cache.TryGetValue(name, out var cached) && cached != null) return cached;

            var sprite = Resources.Load<Sprite>("Sprites/" + name);
            if (sprite == null) sprite = CreateFallback(fallbackColor);

            Cache[name] = sprite;
            return sprite;
        }

        static Sprite CreateFallback(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            // pixelsPerUnit = 1：让 1×1 像素的纯色方块正好是 1 个世界单位（填满一格）
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
