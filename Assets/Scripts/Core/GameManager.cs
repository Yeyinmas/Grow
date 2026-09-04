using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrowGame
{
    /// <summary>游戏进行阶段。</summary>
    public enum GamePhase
    {
        PlayerTurn,  // 玩家回合：等待输入
        MonsterTurn, // 怪物回合：怪物依次行动
        Won,         // 胜利
        Lost         // 失败
    }

    /// <summary>
    /// 游戏核心：持有地图格子、玩家、怪物，驱动回合流程，
    /// 处理道具、开关/门、胜负判断与场景切换。
    /// 挂到一个场景级的空物体上（例如 "GameManager"）。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GridCell[,] Grid { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        [Header("地图")]
        [Tooltip("格子之间的间距（世界单位），决定地图大小。")]
        public float tileSize = 1f;

        [Tooltip("每个方块/贴图的显示大小（相对格子的倍数，1 = 正好填满一格）。只影响方块视觉大小，不影响间距。")]
        public float spriteScale = 1f;

        [Header("实体")]
        public PlayerController player;
        public List<MonsterController> monsters = new List<MonsterController>();

        [Header("道具配置")]
        public int bushCount = 2;      // 灌木丛初始数量
        public int trapCount = 1;      // 困怪陷阱初始数量
        public int bushGrowTurns = 2;  // 灌木丛生长 X 回合后堵路
        public int trapHoldTurns = 2;  // 陷阱困住怪物 X 回合

        [Header("流程")]
        public float monsterMoveDelay = 0.15f; // 怪物行动之间的展示延迟（秒）

        [Header("藤蔓")]
        [Tooltip("藤蔓源未在关卡文本中指定长度时，使用的默认生长格数。")]
        public int defaultVineLength = 4;

        [Header("场景切换")]
        public string nextSceneName = ""; // 留空则加载 Build Settings 中的下一个场景
        public float winDelay = 1.5f;

        public GamePhase Phase { get; private set; } = GamePhase.PlayerTurn;
        public int Turn { get; private set; } = 0;

        private readonly List<PlacedItem> placedItems = new List<PlacedItem>();
        private readonly List<Vector2Int> doorCells = new List<Vector2Int>();

        // 藤蔓运行时状态：与 vineSources 平行，记录每个藤蔓已生长格数
        private readonly List<VineSourceData> vines = new List<VineSourceData>();
        private readonly List<int> vineGrown = new List<int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
        }

        /// <summary>重新加载当前场景，重开本关。</summary>
        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---------- 坐标工具 ----------

        public Vector3 CellToWorld(Vector2Int c) => new Vector3(c.x * tileSize, -c.y * tileSize, 0f);
        public Vector3 CellToWorld(int x, int y) => new Vector3(x * tileSize, -y * tileSize, 0f);

        /// <summary>按 spriteScale 缩放物体，让方块视觉大小可调。</summary>
        public void ApplyVisualScale(GameObject go)
        {
            go.transform.localScale = Vector3.one * spriteScale;
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        public bool IsWalkable(Vector2Int c)
        {
            if (!InBounds(c)) return false;
            return Grid[c.x, c.y].IsWalkable;
        }

        public bool IsMonsterAt(Vector2Int c)
        {
            foreach (var m in monsters)
                if (m != null && m.Coord == c) return true;
            return false;
        }

        // ---------- 初始化（由 MapGenerator 调用） ----------

        public void Setup(GridCell[,] cells, PlayerController p, List<MonsterController> mons, List<VineSourceData> vineSources)
        {
            Grid = cells;
            Width = cells.GetLength(0);
            Height = cells.GetLength(1);
            player = p;
            monsters = mons;

            doorCells.Clear();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (Grid[x, y].Type == TileType.Door)
                        doorCells.Add(new Vector2Int(x, y));

            vines.Clear();
            vineGrown.Clear();
            foreach (var v in vineSources)
            {
                if (v.MaxLength <= 0) v.MaxLength = defaultVineLength; // 未指定则用默认长度
                vines.Add(v);
                vineGrown.Add(0);
            }

            BeginPlayerTurn();
        }

        // ---------- 玩家回合 ----------

        void BeginPlayerTurn()
        {
            Phase = GamePhase.PlayerTurn;
            Turn++;

            if (!HasAnyLegalMove())
            {
                Lose("玩家被困住，无法行动！");
            }
        }

        bool HasAnyLegalMove()
        {
            foreach (var dir in Pathfinding.Directions)
            {
                var t = player.Coord + dir;
                if (IsWalkable(t) && !IsMonsterAt(t)) return true;
            }
            return false;
        }

        public bool TryMovePlayer(Vector2Int dir)
        {
            if (Phase != GamePhase.PlayerTurn) return false;

            var target = player.Coord + dir;
            if (!IsWalkable(target)) return false;

            // 主动走进怪物格：视为被抓住
            if (IsMonsterAt(target))
            {
                player.MoveTo(target);
                Lose("被怪物抓住了！");
                return true;
            }

            player.MoveTo(target);

            if (Grid[target.x, target.y].Type == TileType.Switch) OpenAllDoors();

            if (Grid[target.x, target.y].Type == TileType.Goal)
            {
                Win();
                return true;
            }

            StartCoroutine(MonsterPhase());
            return true;
        }

        // ---------- 道具 ----------

        public bool TryPlaceItem(ItemKind kind)
        {
            if (Phase != GamePhase.PlayerTurn) return false;

            var cell = Grid[player.Coord.x, player.Coord.y];
            if (cell.Type != TileType.ItemLocation) return false; // 只能在道具使用地放置
            if (cell.PlacedItem != null) return false;            // 一格只能放一个道具

            if (kind == ItemKind.Bush && bushCount <= 0) return false;
            if (kind == ItemKind.Trap && trapCount <= 0) return false;

            var item = new PlacedItem { Kind = kind, Pos = player.Coord };
            if (kind == ItemKind.Bush)
            {
                bushCount--;
                item.GrowTurns = bushGrowTurns;
            }
            else
            {
                trapCount--;
            }

            cell.PlacedItem = item;
            item.Visual = CreateItemVisual(item);
            placedItems.Add(item);
            return true;
        }

        GameObject CreateItemVisual(PlacedItem item)
        {
            var go = new GameObject(item.Kind == ItemKind.Bush ? "Bush" : "Trap");
            go.transform.position = CellToWorld(item.Pos);
            ApplyVisualScale(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            if (item.Kind == ItemKind.Bush)
                sr.sprite = SpriteLibrary.Get("item_bush", new Color(0.25f, 0.70f, 0.30f));
            else
                sr.sprite = SpriteLibrary.Get("item_trap", new Color(0.80f, 0.50f, 0.20f));
            return go;
        }

        void RemoveItem(PlacedItem item)
        {
            if (item.Visual != null) Destroy(item.Visual);
            var cell = Grid[item.Pos.x, item.Pos.y];
            if (cell != null && cell.PlacedItem == item) cell.PlacedItem = null;
        }

        // ---------- 怪物回合 ----------

        IEnumerator MonsterPhase()
        {
            Phase = GamePhase.MonsterTurn;
            yield return new WaitForSeconds(monsterMoveDelay);

            foreach (var m in monsters)
            {
                if (m == null) continue;

                // 被困：跳过本回合行动
                if (m.TrappedTurns > 0)
                {
                    m.TrappedTurns--;
                    continue;
                }

                Vector2Int? next = Pathfinding.NextStepTowards(m.Coord, player.Coord, Width, Height, IsWalkable);
                if (next == null) continue; // 无路可走

                Vector2Int n = next.Value;
                m.MoveTo(n);

                // 追到玩家 -> 失败
                if (n == player.Coord)
                {
                    Lose("被怪物抓住了！");
                    yield break;
                }

                // 踩入陷阱 -> 被困并消耗陷阱
                var cell = Grid[n.x, n.y];
                if (cell.PlacedItem != null && cell.PlacedItem.Kind == ItemKind.Trap && !cell.PlacedItem.Triggered)
                {
                    cell.PlacedItem.Triggered = true;
                    m.TrappedTurns = trapHoldTurns;
                    RemoveItem(cell.PlacedItem);
                }
            }

            EndRound();
            BeginPlayerTurn();
        }

        // ---------- 回合推进：灌木丛生长 ----------

        void EndRound()
        {
            for (int i = placedItems.Count - 1; i >= 0; i--)
            {
                var it = placedItems[i];
                if (it.Kind != ItemKind.Bush) continue;

                it.GrowTurns--;
                if (it.GrowTurns <= 0)
                {
                    Grid[it.Pos.x, it.Pos.y].SetBlocked(true); // 堵住该格
                    placedItems.RemoveAt(i);
                    RemoveItem(it);
                }
            }

            GrowVines();
        }

        // ---------- 藤蔓生长 ----------

        void GrowVines()
        {
            for (int i = 0; i < vines.Count; i++)
            {
                var v = vines[i];
                if (vineGrown[i] >= v.MaxLength) continue; // 已长到上限，停止

                // 下一个预期生长的位置
                Vector2Int next = v.Pos + DirectionVec(v.Direction) * (vineGrown[i] + 1);

                // 仅当下一格是可达节点（道路），且没被玩家/怪物占用时才生长
                if (!CanVineGrow(next)) continue;

                Grid[next.x, next.y].SetVine(true);
                vineGrown[i]++;
            }
        }

        bool CanVineGrow(Vector2Int c)
        {
            if (!InBounds(c)) return false;

            var cell = Grid[c.x, c.y];
            if (cell.Type != TileType.Walkable) return false; // 只能长到道路格
            if (!cell.IsWalkable) return false;               // 已被堵住
            if (player != null && player.Coord == c) return false; // 玩家占用
            foreach (var m in monsters)
                if (m != null && m.Coord == c) return false;       // 怪物占用
            return true;
        }

        static Vector2Int DirectionVec(VineDirection d)
        {
            switch (d)
            {
                case VineDirection.Left:  return new Vector2Int(-1, 0);
                case VineDirection.Right: return new Vector2Int(1, 0);
                case VineDirection.Up:    return new Vector2Int(0, -1);
                case VineDirection.Down:  return new Vector2Int(0, 1);
                default:                  return Vector2Int.zero;
            }
        }

        // ---------- 门 / 开关 ----------

        void OpenAllDoors()
        {
            foreach (var c in doorCells) Grid[c.x, c.y].SetDoorOpen(true);
        }

        // ---------- 胜负与场景切换 ----------

        void Win()
        {
            if (Phase == GamePhase.Won) return;
            Phase = GamePhase.Won;
            Debug.Log("胜利！到达终点。");
            StartCoroutine(LoadNextSceneAfterDelay());
        }

        void Lose(string reason)
        {
            if (Phase == GamePhase.Lost) return;
            Phase = GamePhase.Lost;
            Debug.Log("失败：" + reason);
        }

        IEnumerator LoadNextSceneAfterDelay()
        {
            yield return new WaitForSeconds(winDelay);

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                int next = SceneManager.GetActiveScene().buildIndex + 1;
                if (next < SceneManager.sceneCountInBuildSettings)
                    SceneManager.LoadScene(next);
            }
        }
    }
}
