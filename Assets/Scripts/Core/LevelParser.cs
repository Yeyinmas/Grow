using System.Collections.Generic;
using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 把关卡文本解析为 <see cref="LevelData"/>。
    /// 每行代表地图一行（从上到下），行内字符代表地块。
    /// </summary>
    public static class LevelParser
    {
        public static LevelData Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError("LevelParser：关卡文本为空。");
                return null;
            }

            var lines = new List<string>();
            foreach (var raw in text.Split('\n'))
            {
                // 去掉换行符与首尾空白
                var line = raw.TrimEnd('\r', '\n').Trim();
                if (string.IsNullOrEmpty(line)) continue;      // 跳过空行
                if (line.StartsWith("#")) continue;             // 支持 # 注释行
                lines.Add(line);
            }

            if (lines.Count == 0)
            {
                Debug.LogError("LevelParser：关卡没有有效行。");
                return null;
            }

            int height = lines.Count;
            int width = 0;
            foreach (var l in lines) width = Mathf.Max(width, l.Length);

            var data = new LevelData { Width = width, Height = height, Tiles = new TileType[width, height] };

            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                for (int x = 0; x < width; x++)
                {
                    // 行不足的地方视为障碍，保证地图矩形完整
                    char ch = x < line.Length ? line[x] : 'X';
                    switch (ch)
                    {
                        case 'R': data.Tiles[x, y] = TileType.Walkable; break;
                        case 'X': data.Tiles[x, y] = TileType.Obstacle; break;
                        case 'S': data.Tiles[x, y] = TileType.Switch; break;
                        case 'G': data.Tiles[x, y] = TileType.Door; break;
                        case 'D': data.Tiles[x, y] = TileType.ItemLocation; break;
                        case 'F': data.Tiles[x, y] = TileType.Goal; break;
                        case 'P': data.Tiles[x, y] = TileType.Walkable; data.PlayerStart = new Vector2Int(x, y); break;
                        case 'E': data.Tiles[x, y] = TileType.Walkable; data.EnemyStarts.Add(new Vector2Int(x, y)); break;

                        // 藤蔓源：V 向左（文档格式），> 向右，^ 向上，v 向下
                        case 'V':
                            data.Tiles[x, y] = TileType.VineSource;
                            data.VineSources.Add(NewVine(x, y, VineDirection.Left));
                            break;
                        case '>':
                            data.Tiles[x, y] = TileType.VineSource;
                            data.VineSources.Add(NewVine(x, y, VineDirection.Right));
                            break;
                        case '^':
                            data.Tiles[x, y] = TileType.VineSource;
                            data.VineSources.Add(NewVine(x, y, VineDirection.Up));
                            break;
                        case 'v':
                            data.Tiles[x, y] = TileType.VineSource;
                            data.VineSources.Add(NewVine(x, y, VineDirection.Down));
                            break;

                        default: data.Tiles[x, y] = TileType.Obstacle; break; // 未知字符视为障碍
                    }
                }
            }

            return data;
        }

        static VineSourceData NewVine(int x, int y, VineDirection dir)
        {
            return new VineSourceData { Pos = new Vector2Int(x, y), Direction = dir, MaxLength = 0 };
        }
    }
}
