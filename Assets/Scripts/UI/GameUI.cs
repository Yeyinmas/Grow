using UnityEngine;

namespace GrowGame
{
    /// <summary>
    /// 简易 HUD：用 OnGUI 显示回合数、道具数量、操作提示与胜负状态。
    /// 正式版本可替换成 Canvas / TextMeshPro UI。
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 320, 240));
            GUILayout.Label($"回合：{gm.Turn}");
            GUILayout.Label($"灌木丛（按 1 放置）：{gm.bushCount}");
            GUILayout.Label($"困怪陷阱（按 2 放置）：{gm.trapCount}");
            GUILayout.Space(8);
            GUILayout.Label("移动：WASD 或方向键");
            GUILayout.Label("放置道具：站在 D 格（蓝色）上按 1 / 2");
            GUILayout.Label("重新开始：按 R");
            GUILayout.Space(8);

            if (gm.Phase == GamePhase.Won)
                GUILayout.Label("<color=green><size=24>胜利！</size></color>");
            else if (gm.Phase == GamePhase.Lost)
                GUILayout.Label("<color=red><size=24>失败！</size></color>");

            GUILayout.EndArea();
        }
    }
}
