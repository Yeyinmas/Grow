using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 地图上的一个格子。挂在由 MapGenerator 生成的每个格子对象上。
    /// 负责自身类型、可通行状态以及显示。
    /// </summary>
    public class GridCell : MonoBehaviour
    {
        public Vector2Int Coord;
        public TileType Type;

        /// <summary>格子上放置的道具（灌木丛 / 陷阱 / 传送道具），无则 null。</summary>
        public PlacedItem PlacedItem;

        /// <summary>门是否已打开（仅 Type == Door 时有效）。</summary>
        public bool IsDoorOpen;

        /// <summary>灌木丛生长完成后，该格被堵住。</summary>
        public bool IsBlocked;

        /// <summary>藤蔓生长到该格后，该格被堵住（藤蔓占用的格子）。</summary>
        public bool IsVine;

        private SpriteRenderer _renderer;
        private SpriteRenderer _vineRenderer; // 藤蔓叠加层（叠在 floor 之上）

        /// <summary>该格当前是否可通行。</summary>
        public bool IsWalkable
        {
            get
            {
                if (Type == TileType.Obstacle) return false;
                if (Type == TileType.VineSource) return false;
                if (Type == TileType.Door) return IsDoorOpen;
                if (IsBlocked) return false;
                if (IsVine) return false;
                return true;
            }
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        public void SetVisual(Sprite sprite) => _renderer.sprite = sprite;

        public void SetDoorOpen(bool open)
        {
            if (Type != TileType.Door) return;
            IsDoorOpen = open;
            _renderer.sprite = SpriteLibrary.Get(
                open ? "tile_door_open" : "tile_door",
                open ? new Color(0.30f, 0.60f, 0.35f) : new Color(0.60f, 0.35f, 0.15f));
        }

        public void SetBlocked(bool blocked)
        {
            IsBlocked = blocked;
            if (blocked)
                _renderer.sprite = SpriteLibrary.Get("tile_obstacle", new Color(0.05f, 0.05f, 0.06f));
        }

        /// <summary>灌木丛成熟：堵住该格（不可通行），底板换成地板（上面会盖成品贴图）。</summary>
        public void SetMatureBush()
        {
            IsBlocked = true;
            _renderer.sprite = SpriteLibrary.Get("tile_floor", new Color(0.18f, 0.20f, 0.24f));
        }

        /// <summary>在 floor 之上叠加藤蔓贴图（不替换 floor），并按生长方向旋转。</summary>
        public void SetVine(Sprite sprite, float rotationZ)
        {
            IsVine = true;
            if (_vineRenderer == null)
            {
                var go = new GameObject("VineOverlay");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                _vineRenderer = go.AddComponent<SpriteRenderer>();
                _vineRenderer.sortingOrder = 1; // 叠在格子基础贴图之上
            }
            _vineRenderer.sprite = sprite;
            _vineRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        /// <summary>把该格变回普通可通行地块（空地 .），并刷新显示。用于未成熟植物被敌人踩掉后的还原。</summary>
        public void SetWalkable()
        {
            Type = TileType.Walkable;
            IsBlocked = false;
            IsVine = false;
            _renderer.sprite = SpriteLibrary.Get("tile_floor", new Color(0.18f, 0.20f, 0.24f));
        }
    }
}
