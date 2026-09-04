using UnityEngine;

namespace GrowGame
{
    /// <summary>藤蔓生长方向。</summary>
    public enum VineDirection
    {
        Left,   // 向左（x - 1）
        Right,  // 向右（x + 1）
        Up,     // 向上（y - 1）
        Down    // 向下（y + 1）
    }

    /// <summary>
    /// 关卡中一个藤蔓源的数据（纯数据）。
    /// 藤蔓从 <see cref="Pos"/> 出发，沿 <see cref="Direction"/> 生长，
    /// 最多长出 <see cref="MaxLength"/> 格后停止。
    /// </summary>
    public class VineSourceData
    {
        public Vector2Int Pos;
        public VineDirection Direction;
        public int MaxLength; // 最多生长多少格
    }
}
