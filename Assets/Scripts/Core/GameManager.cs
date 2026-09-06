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
        Lost,        // 失败
        LevelIntro   // 关卡开始说明
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
        [Tooltip("玩家/怪物贴图相对格子中心向上偏移的格数（角色比格子高，往上移一点让脚踩在格子上）。")]
        public float characterYOffset = 0.2f;

        public PlayerController player;
        public List<MonsterController> monsters = new List<MonsterController>();

        [Header("道具配置")]
        public int bushCount = 2;      // 灌木丛初始数量
        public int trapCount = 1;      // 困怪陷阱初始数量
        public int portalCount = 1;    // 传送道具初始数量
        public int bushGrowTurns = 3;  // 灌木丛生长 X 回合后堵路
        public int trapGrowTurns = 2;  // 陷阱生长 X 回合后成熟（成熟前被怪物踩过会被踩掉）
        public int trapHoldTurns = 2;  // 陷阱困住怪物 X 回合

        [Header("关卡开始说明")]
        public bool showLevelIntro = false;
        public string introSpriteName = "tile_portal";
        public string introTitle = "新道具";
        [TextArea(2, 6)]
        public string introDescription = "在这里填写道具的使用方法。";

        [Header("流程")]
        [Tooltip("阶段之间的展示间隔（秒）：玩家移动 → 植物生长 → 敌人移动 之间各等待该时长，期间玩家输入无效。")]
        public float stageDelay = 0.1f;

        [Tooltip("每个怪物行动之间的展示间隔（秒）。")]
        public float monsterMoveDelay = 0.15f;

        [Header("梳子 / 仙人掌")]
        [Tooltip("持梳子时，靠近仙人掌（门）多少格内会自动使用（曼哈顿距离；1 = 上下左右相邻四格）。")]
        public int combProximity = 1;

        [Header("藤蔓")]
        [Tooltip("藤蔓源未在关卡文本中指定长度时，使用的默认生长格数。")]
        public int defaultVineLength = 4;

        [Header("音乐")]
        [Tooltip("背景音乐文件名（Assets/Resources/Music/{name}），留空则不播放。")]
        public string musicClipName = "Project";

        [Header("场景切换")]
        public string nextSceneName = ""; // 留空则加载 Build Settings 中的下一个场景
        [Tooltip("按 Esc 返回的开始菜单场景名。")]
        public string menuSceneName = "menu";
        public float winDelay = 1.5f;

        public GamePhase Phase { get; private set; } = GamePhase.PlayerTurn;
        public int Turn { get; private set; } = 0;

        /// <summary>玩家是否已捡起梳子（开关）。</summary>
        public bool HasComb { get; private set; }

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

        private void Start()
        {
            if (!string.IsNullOrEmpty(musicClipName))
                AudioManager.PlayMusic(musicClipName);
        }

        private void Update()
        {
            if (Phase == GamePhase.LevelIntro && Input.GetKeyDown(KeyCode.E))
                BeginPlayerTurn();
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.Escape) && !string.IsNullOrEmpty(menuSceneName))
                SceneManager.LoadScene(menuSceneName);
        }

        /// <summary>重新加载当前场景，重开本关。</summary>
        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadMenu()
        {
            if (!string.IsNullOrEmpty(menuSceneName))
                SceneManager.LoadScene(menuSceneName);
        }

        public bool CanLoadAdjacentLevel(int offset)
        {
            return TryGetAdjacentLevelName(offset, out _);
        }

        public void LoadAdjacentLevel(int offset)
        {
            if (TryGetAdjacentLevelName(offset, out string sceneName))
                SceneManager.LoadScene(sceneName);
        }

        bool TryGetAdjacentLevelName(int offset, out string sceneName)
        {
            sceneName = null;
            string current = SceneManager.GetActiveScene().name;
            if (!current.StartsWith("level")) return false;
            if (!int.TryParse(current.Substring(5), out int levelNumber)) return false;

            int targetNumber = levelNumber + offset;
            if (targetNumber <= 0) return false;

            sceneName = "level" + targetNumber;
            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        // ---------- 坐标工具 ----------

        public Vector3 CellToWorld(Vector2Int c) => new Vector3(c.x * tileSize, -c.y * tileSize, 0f);
        public Vector3 CellToWorld(int x, int y) => new Vector3(x * tileSize, -y * tileSize, 0f);

        /// <summary>角色贴图的世界坐标：在格子中心基础上向上偏移 characterYOffset 格。</summary>
        public Vector3 CharacterWorld(Vector2Int c) => CellToWorld(c) + new Vector3(0f, characterYOffset * tileSize, 0f);

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

            // 藤蔓源所在格：floor 之上叠藤蔓身体
            foreach (var v in vines)
                SetVineBody(v.Pos, v.Direction);

            if (showLevelIntro)
                Phase = GamePhase.LevelIntro;
            else
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
            if (dir == Vector2Int.zero) return false;

            var originCell = Grid[player.Coord.x, player.Coord.y];
            var portal = originCell.PlacedItem != null && originCell.PlacedItem.Kind == ItemKind.Portal
                ? originCell.PlacedItem
                : null;

            var target = player.Coord + dir;
            if (portal != null)
            {
                target = player.Coord;
                var next = target + dir;
                while (IsWalkable(next))
                {
                    target = next;
                    if (IsMonsterAt(next)) break;
                    next += dir;
                }

                if (target == player.Coord) return false;
            }
            else if (!IsWalkable(target))
            {
                return false;
            }

            // 主动走进怪物格：视为被抓住
            if (IsMonsterAt(target))
            {
                player.MoveTo(target);
                Lose("被怪物抓住了！");
                return true;
            }

            player.MoveTo(target);
            AudioManager.Play("player_step");

            // 捡起梳子（原开关）
            if (Grid[target.x, target.y].Type == TileType.Switch) PickUpComb();

            // 靠近仙人掌时自动使用梳子
            TryUseComb();

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
            if (kind == ItemKind.Portal && portalCount <= 0) return false;

            var item = new PlacedItem { Kind = kind, Pos = player.Coord };
            if (kind == ItemKind.Bush)
            {
                bushCount--;
                item.GrowTurns = bushGrowTurns;
            }
            else if (kind == ItemKind.Trap)
            {
                trapCount--;
                item.GrowTurns = trapGrowTurns; // 陷阱也需要生长时间
            }
            else
            {
                portalCount--;
                item.GrowTurns = 0;
            }

            cell.PlacedItem = item;
            item.Visual = CreateItemVisual(item);
            placedItems.Add(item);
            AudioManager.Play("plant_place");

            StartCoroutine(MonsterPhase()); // 使用道具同样消耗一回合
            return true;
        }

        GameObject CreateItemVisual(PlacedItem item)
        {
            string objectName = item.Kind == ItemKind.Bush ? "Bush" :
                                item.Kind == ItemKind.Trap ? "Trap" : "Portal";
            var go = new GameObject(objectName);
            go.transform.position = CellToWorld(item.Pos);
            ApplyVisualScale(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            sr.sprite = item.Kind == ItemKind.Portal
                ? SpriteLibrary.Get("tile_portal", new Color(0.55f, 0.30f, 0.90f))
                : SpriteLibrary.Get("tile_item_wait", new Color(0.45f, 0.72f, 0.35f));
            return go;
        }

        /// <summary>植物成熟后换成成品贴图。</summary>
        void ShowMatureItem(PlacedItem item)
        {
            if (item.Visual == null) return;
            var sr = item.Visual.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            if (item.Kind == ItemKind.Bush)
                sr.sprite = SpriteLibrary.Get("item_bush", new Color(0.25f, 0.70f, 0.30f));
            else if (item.Kind == ItemKind.Trap)
                sr.sprite = SpriteLibrary.Get("item_trap", new Color(0.80f, 0.50f, 0.20f));
            else
                sr.sprite = SpriteLibrary.Get("tile_portal", new Color(0.55f, 0.30f, 0.90f));
        }

        void RemoveItem(PlacedItem item)
        {
            placedItems.Remove(item); // 从生长列表移除，防止被踩掉的植物之后又“成熟”堵路
            if (item.Visual != null) Destroy(item.Visual);
            var cell = Grid[item.Pos.x, item.Pos.y];
            if (cell != null && cell.PlacedItem == item) cell.PlacedItem = null;
        }

        // ---------- 怪物回合 ----------

        IEnumerator MonsterPhase()
        {
            Phase = GamePhase.MonsterTurn; // 立刻进入怪物回合，屏蔽玩家输入

            // 行动顺序：玩家动 → 植物长一回合 → 敌人动，阶段之间各停 stageDelay 秒
            yield return new WaitForSeconds(stageDelay);
            GrowPlants();

            yield return new WaitForSeconds(stageDelay);

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
                    var caughtCell = Grid[n.x, n.y];
                    if (caughtCell.PlacedItem != null && caughtCell.PlacedItem.Kind == ItemKind.Portal)
                    {
                        RemoveItem(caughtCell.PlacedItem);
                        caughtCell.SetWalkable();
                    }
                    Lose("被怪物抓住了！");
                    yield break;
                }

                AudioManager.Play("monster_step");

                // 敌人经过该格：处理格子上的植物
                var cell = Grid[n.x, n.y];
                var item = cell.PlacedItem;
                if (item != null)
                {
                    if (item.GrowTurns > 0)
                    {
                        // 未成熟的植物（灌木丛/陷阱）被踩过：被踩掉，该格变回普通空地（.），不再是可种植的田地（D）
                        RemoveItem(item);
                        cell.SetWalkable();
                    }
                    else if (item.Kind == ItemKind.Trap && !item.Triggered)
                    {
                        // 成熟陷阱：踩入即触发，困住怪物并消耗陷阱，该格变回空地（.）
                        AudioManager.Play("trap_trigger"); // 大叔吃小土豆
                        item.Triggered = true;
                        m.TrappedTurns = trapHoldTurns;
                        RemoveItem(item);
                        cell.SetWalkable();
                    }
                    else if (item.Kind == ItemKind.Portal)
                    {
                        RemoveItem(item);
                        cell.SetWalkable();
                    }
                }

                yield return new WaitForSeconds(monsterMoveDelay); // 每个怪物之间的小间隔
            }

            GrowVines();
            BeginPlayerTurn();
        }

        // ---------- 回合推进：植物生长（在敌人移动之前） ----------

        void GrowPlants()
        {
            for (int i = placedItems.Count - 1; i >= 0; i--)
            {
                var it = placedItems[i];
                if (it.Kind == ItemKind.Portal) continue;

                it.GrowTurns--;
                if (it.GrowTurns > 0) continue; // 尚未成熟，继续生长

                if (it.GrowTurns == 0) AudioManager.Play("plant_grow"); // 刚好长成（归零）时播放

                // 成熟：灌木丛堵路并显示石楠花成品；陷阱换成成品外观，等怪物踩入触发
                if (it.Kind == ItemKind.Bush)
                {
                    Grid[it.Pos.x, it.Pos.y].SetMatureBush(); // 堵住该格，底板换成地板
                    ShowMatureItem(it);                        // 换成石楠花成品贴图
                    placedItems.RemoveAt(i);                   // 之后不再参与生长
                }
                else
                {
                    ShowMatureItem(it); // 陷阱成熟：换成成品贴图
                }
            }

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

                // 旧的尖端退化为藤蔓身体，新格成为新的尖端
                if (vineGrown[i] > 0)
                {
                    Vector2Int prevTip = v.Pos + DirectionVec(v.Direction) * vineGrown[i];
                    SetVineBody(prevTip, v.Direction);
                }

                var nextCell = Grid[next.x, next.y];
                if (nextCell.Type == TileType.ItemLocation)
                {
                    if (nextCell.PlacedItem != null)
                        RemoveItem(nextCell.PlacedItem);
                    nextCell.SetWalkable();
                }

                SetVineTip(next, v.Direction);
                vineGrown[i]++;
            }
        }

        bool CanVineGrow(Vector2Int c)
        {
            if (!InBounds(c)) return false;

            var cell = Grid[c.x, c.y];
            bool isDestructibleItemCell = cell.Type == TileType.ItemLocation &&
                (cell.PlacedItem == null || cell.PlacedItem.Kind != ItemKind.Bush);
            if (cell.Type != TileType.Walkable && !isDestructibleItemCell) return false;
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

        // ---------- 藤蔓渲染 ----------

        /// <summary>藤蔓贴图默认朝上，按生长方向旋转：上 0°、左 90°、右 -90°、下 180°。</summary>
        static float VineRotation(VineDirection d)
        {
            switch (d)
            {
                case VineDirection.Up:    return 0f;
                case VineDirection.Left:  return 90f;
                case VineDirection.Right: return -90f;
                case VineDirection.Down:  return 180f;
                default:                  return 0f;
            }
        }

        void SetVineBody(Vector2Int c, VineDirection dir)
        {
            Grid[c.x, c.y].SetVine(SpriteLibrary.Get("tile_vine", new Color(0.25f, 0.55f, 0.20f)), VineRotation(dir));
        }

        void SetVineTip(Vector2Int c, VineDirection dir)
        {
            // tile_vine_source 图其实是藤蔓尖尖，朝向生长方向
            Grid[c.x, c.y].SetVine(SpriteLibrary.Get("tile_vine_source", new Color(0.55f, 0.30f, 0.15f)), VineRotation(dir));
        }

        // ---------- 梳子 / 仙人掌 ----------

        /// <summary>捡起梳子：梳子从地图上消失，玩家获得梳子（道具栏第三格显示）。</summary>
        void PickUpComb()
        {
            if (HasComb) return;
            HasComb = true;
            Grid[player.Coord.x, player.Coord.y].SetWalkable(); // 梳子所在格变回普通地面
        }

        /// <summary>若持有梳子且靠近仙人掌（门），自动使用梳子把仙人掌打开。</summary>
        void TryUseComb()
        {
            if (!HasComb) return;

            bool used = false;
            foreach (var c in doorCells)
            {
                var cell = Grid[c.x, c.y];
                if (cell.IsDoorOpen) continue; // 已打开
                if (Manhattan(player.Coord, c) <= combProximity)
                {
                    cell.SetDoorOpen(true); // 仙人掌打开
                    used = true;
                }
            }

            if (used)
            {
                HasComb = false;
                AudioManager.Play("comb_return"); // 梳子还给仙人掌
            }
        }

        static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // ---------- 胜负与场景切换 ----------

        void Win()
        {
            if (Phase == GamePhase.Won) return;
            Phase = GamePhase.Won;
            AudioManager.Play("win");
            Debug.Log("胜利！到达终点。");
            StartCoroutine(LoadNextSceneAfterDelay());
        }

        void Lose(string reason)
        {
            if (Phase == GamePhase.Lost) return;
            Phase = GamePhase.Lost;
            AudioManager.Play("lose"); // 大叔抓到
            Debug.Log("失败：" + reason);
            StartCoroutine(PlayBroScream());
        }

        // 被抓住时：先放大叔抓到，间隔一小会儿再放土豆惨叫
        IEnumerator PlayBroScream()
        {
            yield return new WaitForSeconds(0.5f);
            AudioManager.Play("bro_scream");
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
