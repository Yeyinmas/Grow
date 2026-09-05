using System.Collections.Generic;
using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 根据关卡文本程序化生成地图：格子、玩家、怪物。
    /// 挂到与 GameManager 同一物体或同一场景的空物体上。
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        [Header("关卡文本")]
        public TextAsset levelFile;           // 拖入 txt 文件（推荐）
        public string levelResourceName = ""; // 或填 Resources 相对路径，如 "Levels/level1"

        [Header("预制体（可留空，留空则自动创建）")]
        public GameObject playerPrefab;
        public GameObject monsterPrefab;

        [Header("摄像机")]
        public Camera cam;
        public bool autoFitCamera = true; // 自动居中并调整视野
        public float cameraPadding = 1.5f;

        [Header("背景")]
        [Tooltip("整张地图的背景图（Resources/Sprites/background），用该颜色叠加；DBDBDB 即把图略微压暗。")]
        public Color backgroundTint = new Color(255f / 255f, 221f / 255f, 166f / 255f, 1f);

        private void Start()
        {
            Generate();
        }

        void Generate()
        {
            string text = levelFile != null ? levelFile.text : null;
            if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(levelResourceName))
            {
                var asset = Resources.Load<TextAsset>(levelResourceName);
                if (asset != null) text = asset.text;
            }

            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError("MapGenerator：没有关卡文本。请在 levelFile 或 levelResourceName 里指定。");
                return;
            }

            var data = LevelParser.Parse(text);
            if (data == null) return;

            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("MapGenerator：场景里缺少 GameManager。");
                return;
            }
            float ts = gm.tileSize;

            // 生成格子
            var cells = new GridCell[data.Width, data.Height];
            var root = new GameObject("Grid");
            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    var go = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(root.transform);
                    go.transform.position = gm.CellToWorld(x, y);
                    gm.ApplyVisualScale(go);

                    var cell = go.AddComponent<GridCell>();
                    cell.Coord = new Vector2Int(x, y);
                    cell.Type = data.Tiles[x, y];
                    cell.SetVisual(SpriteFor(cell.Type));
                    cells[x, y] = cell;
                }
            }

            // 生成玩家
            var player = SpawnPlayer(gm, data.PlayerStart);

            // 生成怪物
            var monsters = new List<MonsterController>();
            foreach (var e in data.EnemyStarts)
                monsters.Add(SpawnMonster(gm, e));

            CreateBackground(data.Width, data.Height, ts);
            FitCamera(data.Width, data.Height, ts);
            gm.Setup(cells, player, monsters, data.VineSources);
        }

        PlayerController SpawnPlayer(GameManager gm, Vector2Int pos)
        {
            PlayerController pc;
            if (playerPrefab != null)
            {
                var go = Instantiate(playerPrefab, gm.CharacterWorld(pos), Quaternion.identity);
                gm.ApplyVisualScale(go);
                pc = go.GetComponent<PlayerController>();
                if (pc == null) pc = go.AddComponent<PlayerController>();
            }
            else
            {
                var go = new GameObject("Player");
                go.transform.position = gm.CharacterWorld(pos);
                gm.ApplyVisualScale(go);
                pc = go.AddComponent<PlayerController>();
            }
            pc.Coord = pos;
            pc.SetVisual(SpriteLibrary.Get("player", new Color(0.20f, 0.90f, 0.90f)));
            return pc;
        }

        MonsterController SpawnMonster(GameManager gm, Vector2Int pos)
        {
            MonsterController mc;
            if (monsterPrefab != null)
            {
                var go = Instantiate(monsterPrefab, gm.CharacterWorld(pos), Quaternion.identity);
                gm.ApplyVisualScale(go);
                mc = go.GetComponent<MonsterController>();
                if (mc == null) mc = go.AddComponent<MonsterController>();
            }
            else
            {
                var go = new GameObject("Monster");
                go.transform.position = gm.CharacterWorld(pos);
                gm.ApplyVisualScale(go);
                mc = go.AddComponent<MonsterController>();
            }
            mc.Coord = pos;
            mc.SetVisual(SpriteLibrary.Get("monster", new Color(0.90f, 0.25f, 0.25f)));
            return mc;
        }

        Sprite SpriteFor(TileType t)
        {
            switch (t)
            {
                case TileType.Obstacle:     return SpriteLibrary.Get("tile_obstacle", new Color(0.05f, 0.05f, 0.06f));
                case TileType.Switch:       return SpriteLibrary.Get("tile_switch", new Color(1.00f, 0.85f, 0.30f));
                case TileType.Door:         return SpriteLibrary.Get("tile_door", new Color(0.60f, 0.35f, 0.15f));
                case TileType.ItemLocation: return SpriteLibrary.Get("tile_item", new Color(0.35f, 0.50f, 0.90f));
                case TileType.Goal:         return SpriteLibrary.Get("tile_goal", new Color(0.20f, 0.90f, 0.40f));
                case TileType.VineSource:   return SpriteLibrary.Get("tile_floor", new Color(0.18f, 0.20f, 0.24f)); // 藤蔓源：floor 之上叠藤蔓（见 GameManager）
                default:                    return SpriteLibrary.Get("tile_floor", new Color(0.18f, 0.20f, 0.24f));
            }
        }

        void FitCamera(int w, int h, float ts)
        {
            if (!autoFitCamera) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            cam.orthographic = true;
            cam.transform.position = new Vector3((w - 1) * ts / 2f, -(h - 1) * ts / 2f, -10f);

            float halfH = h * ts / 2f + cameraPadding;
            float halfW = w * ts / 2f + cameraPadding;
            cam.orthographicSize = Mathf.Max(halfH, halfW / cam.aspect);
        }

        /// <summary>创建铺满整个地图的背景图，用 backgroundTint 上色（DBDBDB 略微压暗）。</summary>
        void CreateBackground(int w, int h, float ts)
        {
            var sprite = SpriteLibrary.Get("background", new Color(0.85f, 0.85f, 0.85f));
            if (sprite == null) return;

            var go = new GameObject("Background");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = backgroundTint;  // DBDBDB：略微压暗
            sr.sortingOrder = -10;      // 位于所有格子之下

            // 铺满整个地图区域：保持宽高比，用「覆盖」缩放让背景填满地图（超出部分作为底图无妨）
            var size = sprite.bounds.size;
            if (size.x <= 0f || size.y <= 0f) return;

            float mapW = w * ts;
            float mapH = h * ts;
            float scale = Mathf.Max(mapW / size.x, mapH / size.y);

            go.transform.position = new Vector3((w - 1) * ts / 2f, -(h - 1) * ts / 2f, 0f);
            go.transform.localScale = Vector3.one * scale;
        }
    }
}
