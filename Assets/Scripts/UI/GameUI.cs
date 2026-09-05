using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 道具栏 HUD：左上角竖向排列道具格，图标右侧为剩余数量。
    /// 第 1/2 格是灌木丛、陷阱；第 3 格是梳子（捡起开关后显示，靠近仙人掌自动使用后消失）。
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        const float Slot = 56f;   // 图标尺寸
        const float CountW = 44f; // 数量列宽
        const float Pad = 12f;    // 边距
        const float Gap = 10f;    // 两个道具格之间间隔

        static readonly Color BushColor = new Color(0.25f, 0.70f, 0.30f);
        static readonly Color TrapColor = new Color(0.80f, 0.50f, 0.20f);
        static readonly Color CombColor = new Color(1.00f, 0.85f, 0.30f);

        GUIStyle _countStyle;
        GUIStyle _hotkeyStyle;
        GUIStyle _hintStyle;
        GUIStyle _winStyle;
        GUIStyle _loseStyle;

        void InitStyles()
        {
            _countStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
            };
            _countStyle.normal.textColor = Color.white;

            _hotkeyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
            };
            _hotkeyStyle.normal.textColor = new Color(0f, 0f, 0f, 0.75f);

            _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            _hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

            _winStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 44,
                fontStyle = FontStyle.Bold,
            };
            _winStyle.normal.textColor = new Color(0.2f, 0.9f, 0.3f);

            _loseStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 44,
                fontStyle = FontStyle.Bold,
            };
            _loseStyle.normal.textColor = new Color(0.95f, 0.2f, 0.2f);
        }

        void OnGUI()
        {
            if (_countStyle == null) InitStyles(); // GUIStyle 只能在 OnGUI 里用 GUI.skin 创建

            var gm = GameManager.Instance;
            if (gm == null) return;

            // 回合数
            GUI.Label(new Rect(Pad, Pad, 160f, 22f), $"回合 {gm.Turn}", _hintStyle);

            // 道具栏：竖向排列
            float barX = Pad;
            float barY = Pad + 26f;
            float slotW = Slot + CountW;

            DrawItemSlot(new Rect(barX, barY, slotW, Slot), "item_bush", gm.bushCount, "1", BushColor);
            DrawItemSlot(new Rect(barX, barY + Slot + Gap, slotW, Slot), "item_trap", gm.trapCount, "2", TrapColor);
            DrawCombSlot(new Rect(barX, barY + (Slot + Gap) * 2f, slotW, Slot), gm.HasComb);

            // 底部提示
            GUI.Label(new Rect(Pad, Screen.height - 30f, 400f, 22f), "站在 D 格上按 1 / 2 放置 · R 重开", _hintStyle);

            // 胜负提示
            if (gm.Phase == GamePhase.Won)
                GUI.Label(new Rect(0f, Screen.height / 2f - 40f, Screen.width, 80f), "胜利！", _winStyle);
            else if (gm.Phase == GamePhase.Lost)
                GUI.Label(new Rect(0f, Screen.height / 2f - 40f, Screen.width, 80f), "失败！", _loseStyle);
        }

        void DrawItemSlot(Rect rect, string key, int count, string hotkey, Color fallback)
        {
            GUI.Box(rect, GUIContent.none);

            // 图标（用完的道具变淡）
            var sp = SpriteLibrary.Get(key, fallback);
            var iconRect = new Rect(rect.x + 4f, rect.y + 4f, Slot - 8f, Slot - 8f);

            var old = GUI.color;
            if (count <= 0) GUI.color = new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(iconRect, sp != null && sp.texture != null ? sp.texture : Texture2D.whiteTexture);
            GUI.color = old;

            // 快捷键角标
            GUI.Label(new Rect(iconRect.x + 2f, iconRect.y, 20f, 18f), hotkey, _hotkeyStyle);

            // 数量（右侧）
            GUI.Label(new Rect(rect.x + Slot, rect.y, CountW, Slot), $"×{count}", _countStyle);
        }

        void DrawCombSlot(Rect rect, bool hasComb)
        {
            GUI.Box(rect, GUIContent.none);

            // 持有梳子才显示（否则为空格）
            if (!hasComb) return;

            var sp = SpriteLibrary.Get("tile_switch", CombColor);
            var iconRect = new Rect(rect.x + 4f, rect.y + 4f, Slot - 8f, Slot - 8f);
            GUI.DrawTexture(iconRect, sp != null && sp.texture != null ? sp.texture : Texture2D.whiteTexture);
        }
    }
}
