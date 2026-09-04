using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 网格寻路：BFS 最短路径，用于怪物追踪玩家。
    /// </summary>
    public static class Pathfinding
    {
        /// <summary>
        /// 四个方向，顺序固定为「上、下、左、右」。
        /// 该顺序同时用于平局时的选择规则，保证结果可预测。
        /// </summary>
        public static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, -1), // 上
            new Vector2Int(0, 1),  // 下
            new Vector2Int(-1, 0), // 左
            new Vector2Int(1, 0),  // 右
        };

        /// <summary>
        /// 计算从 <paramref name="from"/> 到 <paramref name="to"/> 的最短路径的下一步。
        /// 返回 null 表示不可达（无路可走）。
        /// </summary>
        public static Vector2Int? NextStepTowards(
            Vector2Int from, Vector2Int to,
            int width, int height,
            Func<Vector2Int, bool> isWalkable)
        {
            if (from == to) return null;

            // 反向 BFS：从目标出发，计算每个格到目标的最短距离
            int[,] dist = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    dist[x, y] = -1;

            if (InBounds(to, width, height) && isWalkable(to))
            {
                dist[to.x, to.y] = 0;
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(to);

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    int d = dist[cur.x, cur.y];
                    foreach (var dir in Directions)
                    {
                        var n = cur + dir;
                        if (!InBounds(n, width, height)) continue;
                        if (dist[n.x, n.y] >= 0) continue;   // 已访问
                        if (!isWalkable(n)) continue;        // 不可通行
                        dist[n.x, n.y] = d + 1;
                        queue.Enqueue(n);
                    }
                }
            }

            // 从 from 的邻居中选距离最小者；平局按 Directions 固定顺序优先
            Vector2Int? best = null;
            int bestDist = int.MaxValue;
            foreach (var dir in Directions)
            {
                var n = from + dir;
                if (!InBounds(n, width, height)) continue;
                if (dist[n.x, n.y] < 0) continue; // 不可达
                if (dist[n.x, n.y] < bestDist)
                {
                    bestDist = dist[n.x, n.y];
                    best = n;
                }
            }
            return best;
        }

        static bool InBounds(Vector2Int c, int w, int h) => c.x >= 0 && c.y >= 0 && c.x < w && c.y < h;
    }
}
