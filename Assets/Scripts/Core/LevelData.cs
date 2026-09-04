using System.Collections.Generic;
using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 解析后的关卡数据，纯数据类，与表现层无关。
    /// </summary>
    public class LevelData
    {
        public int Width;
        public int Height;

        /// <summary>Tiles[x, y]，y = 0 为文本第一行（地图最上方）。</summary>
        public TileType[,] Tiles;

        public Vector2Int PlayerStart;
        public List<Vector2Int> EnemyStarts = new List<Vector2Int>();

        /// <summary>所有藤蔓源。</summary>
        public List<VineSourceData> VineSources = new List<VineSourceData>();
    }
}
