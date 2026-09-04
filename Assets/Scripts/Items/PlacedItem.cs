using UnityEngine;

namespace GrowGame
{
    /// <summary>道具种类。</summary>
    public enum ItemKind
    {
        Bush, // 道具一：灌木丛，生长 X 回合后堵住一块路
        Trap  // 道具二：困怪陷阱，生长 X 回合后成熟，怪物踩入被困
    }

    /// <summary>
    /// 已放置在地图上的道具实例（纯数据 + 显示对象引用）。
    /// 由 GameManager 统一管理和推进。
    /// </summary>
    public class PlacedItem
    {
        public ItemKind Kind;
        public Vector2Int Pos;

        /// <summary>剩余生长回合（灌木丛与陷阱通用），减到 0 后成熟。成熟前被敌人踩过会被踩掉。</summary>
        public int GrowTurns;

        /// <summary>陷阱是否已被怪物触发（触发后消耗）。</summary>
        public bool Triggered;

        /// <summary>道具的显示对象，移除时销毁。</summary>
        public GameObject Visual;
    }
}
