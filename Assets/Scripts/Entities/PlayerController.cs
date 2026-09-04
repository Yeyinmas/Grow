using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 玩家。负责读取输入（移动 / 放置道具）并交给 GameManager 处理。
    /// 做成预制体后挂到玩家对象上。
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public Vector2Int Coord;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = 10; // 保证显示在格子之上
        }

        public void SetVisual(Sprite sprite) => _renderer.sprite = sprite;

        public void MoveTo(Vector2Int c)
        {
            Coord = c;
            if (GameManager.Instance != null)
                transform.position = GameManager.Instance.CellToWorld(c);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.Phase != GamePhase.PlayerTurn) return;

            // 放置道具：站在道具使用地（D 格）上时按 1 / 2
            if (Input.GetKeyDown(KeyCode.Alpha1)) gm.TryPlaceItem(ItemKind.Bush);
            if (Input.GetKeyDown(KeyCode.Alpha2)) gm.TryPlaceItem(ItemKind.Trap);

            // 移动：WASD 或方向键，一次只能选一个方向
            Vector2Int dir = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = new Vector2Int(0, -1);
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = new Vector2Int(0, 1);
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = new Vector2Int(-1, 0);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = new Vector2Int(1, 0);

            if (dir != Vector2Int.zero) gm.TryMovePlayer(dir);
        }
    }
}
