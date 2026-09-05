using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrowGame
{
    /// <summary>
    /// 开始界面：显示标题《土豆发芽了！》和「开始 / 退出」两个按钮。
    /// 挂在菜单场景的一个空物体上即可（场景里需要有 Main Camera，自带 AudioListener）。
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Tooltip("点「开始」后加载的第一关场景名。")]
        public string firstLevelSceneName = "level1";

        [Tooltip("背景音乐文件名（Assets/Resources/Music/{name}），留空则不播放。")]
        public string musicClipName = "Project";

        GUIStyle _titleStyle;
        GUIStyle _buttonStyle;

        void Start()
        {
            if (!string.IsNullOrEmpty(musicClipName))
                AudioManager.PlayMusic(musicClipName);
        }

        void InitStyles()
        {
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 52,
                fontStyle = FontStyle.Bold,
            };
            _titleStyle.normal.textColor = new Color(0.30f, 0.48f, 0.15f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
            };
        }

        void OnGUI()
        {
            if (_titleStyle == null) InitStyles(); // GUIStyle 只能在 OnGUI 里用 GUI.skin 创建

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;
            float bw = 240f;
            float bh = 60f;

            // 标题
            GUI.Label(new Rect(0f, cy - 170f, Screen.width, 80f), "《土豆发芽了！》", _titleStyle);

            // 开始
            if (GUI.Button(new Rect(cx - bw / 2f, cy - 30f, bw, bh), "开 始", _buttonStyle))
                SceneManager.LoadScene(firstLevelSceneName);

            // 退出
            if (GUI.Button(new Rect(cx - bw / 2f, cy + 50f, bw, bh), "退 出", _buttonStyle))
                Quit();
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
