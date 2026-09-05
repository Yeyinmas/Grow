using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 怪物。移动逻辑由 GameManager 在怪物回合调用（最短路径追踪玩家）。
    /// 本类只负责记录位置、被困状态和显示。
    /// </summary>
    public class MonsterController : MonoBehaviour
    {
        public Vector2Int Coord;

        /// <summary>剩余被困回合，>0 时本回合不移动。</summary>
        public int TrappedTurns;

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
                transform.position = GameManager.Instance.CharacterWorld(c);
        }
    }
}
