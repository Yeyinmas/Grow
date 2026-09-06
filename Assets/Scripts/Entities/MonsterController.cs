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
        private GUIStyle _intentStyle;
        private GUIStyle _intentShadowStyle;

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

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            var cam = Camera.main;
            if (gm == null || gm.player == null || cam == null) return;
            if (gm.Phase == GamePhase.LevelIntro) return;

            string label = GetIntentLabel(gm);
            if (string.IsNullOrEmpty(label)) return;

            if (_intentStyle == null)
            {
                _intentStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                _intentStyle.normal.textColor = Color.white;

                _intentShadowStyle = new GUIStyle(_intentStyle);
                _intentShadowStyle.normal.textColor = Color.black;

            }

            bool isArrow = label == "←" || label == "→" || label == "↑" || label == "↓";
            _intentStyle.fontSize = isArrow ? 24 : 20;
            _intentShadowStyle.fontSize = _intentStyle.fontSize;

            Vector3 anchor = _renderer != null && _renderer.sprite != null
                ? new Vector3(_renderer.bounds.min.x - 0.1f, _renderer.bounds.max.y + 0.1f, transform.position.z)
                : transform.position + new Vector3(-0.1f, 0.1f, 0f);
            Vector3 screen = cam.WorldToScreenPoint(anchor);
            if (screen.z <= 0f) return;

            var rect = new Rect(screen.x - 28f, Screen.height - screen.y - 14f, 56f, 28f);
            for (int x = -2; x <= 2; x += 2)
                for (int y = -2; y <= 2; y += 2)
                    if (x != 0 || y != 0)
                        GUI.Label(new Rect(rect.x + x, rect.y + y, rect.width, rect.height), label, _intentShadowStyle);
            GUI.Label(rect, label, _intentStyle);
        }

        private string GetIntentLabel(GameManager gm)
        {
            if (TrappedTurns > 0) return $"X({TrappedTurns})";

            Vector2Int? next = Pathfinding.NextStepTowards(
                Coord, gm.player.Coord, gm.Width, gm.Height, gm.IsWalkable);
            if (next == null) return "—";

            Vector2Int dir = next.Value - Coord;
            if (dir.x < 0) return "←";
            if (dir.x > 0) return "→";
            if (dir.y < 0) return "↑";
            return "↓";
        }
    }
}
