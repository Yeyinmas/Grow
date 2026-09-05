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

        [Header("背景")]
        public Color backgroundTint = Color.white;
        public float backgroundTileScale = 1f;

        [Header("开场动画")]
        [Tooltip("四格漫画图片名，按第 1、2、3、4 张的顺序填写，对应 Assets/Resources/Sprites/{name}。")]
        public string[] openingImageNames = new string[0];
        public float openingPanelEnterDuration = 0.7f;
        public float openingPanelInterval = 0.25f;
        [Range(0f, 1f)]
        public float openingFirstThreeEnterScreenRatio = 0.2f;
        public float openingFourthEnterDuration = 1f;
        public float openingGroupShiftDuration = 0.8f;
        [Range(0f, 1f)]
        public float openingGroupShiftScreenRatio = 0.25f;
        [Range(-1f, 1f)]
        public float openingHorizontalOffsetScreenRatio = 0f;
        public float openingImageScale = 1f;
        public float openingFinalHoldDuration = 2f;

        GUIStyle _titleStyle;
        GUIStyle _buttonStyle;
        GUIStyle _skipStyle;
        SpriteRenderer _backgroundRenderer;
        Camera _camera;
        int _lastScreenWidth = -1;
        int _lastScreenHeight = -1;
        bool _isPlayingOpening;
        float _openingImageStartTime;

        void Start()
        {
            CreateBackground();
            if (!string.IsNullOrEmpty(musicClipName))
                AudioManager.PlayMusic(musicClipName);
        }

        void LateUpdate()
        {
            if (_backgroundRenderer == null) return;
            if (_lastScreenWidth == Screen.width && _lastScreenHeight == Screen.height) return;

            FitBackgroundToCamera();
        }

        void Update()
        {
            if (!_isPlayingOpening) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                FinishOpening();
                return;
            }

            float enter = Mathf.Max(0.01f, openingPanelEnterDuration);
            float interval = Mathf.Max(0f, openingPanelInterval);
            float fourthStart = enter * 3f + interval * 2f;
            float shiftDuration = Mathf.Max(0.01f, openingGroupShiftDuration);
            float totalDuration = fourthStart +
                                  Mathf.Max(0.01f, openingFourthEnterDuration) +
                                  shiftDuration +
                                  Mathf.Max(0f, openingFinalHoldDuration);
            if (Time.unscaledTime - _openingImageStartTime < totalDuration) return;

            FinishOpening();
        }

        void CreateBackground()
        {
            var sprite = SpriteLibrary.Get("background", Color.white);
            if (sprite == null) return;

            var go = new GameObject("Background");
            _backgroundRenderer = go.AddComponent<SpriteRenderer>();
            _backgroundRenderer.sprite = sprite;
            _backgroundRenderer.color = backgroundTint;
            _backgroundRenderer.sortingOrder = -10;
            _backgroundRenderer.drawMode = SpriteDrawMode.Tiled;
            _backgroundRenderer.tileMode = SpriteTileMode.Continuous;
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, backgroundTileScale);

            FitBackgroundToCamera();
        }

        void FitBackgroundToCamera()
        {
            if (_backgroundRenderer == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _camera.backgroundColor = Color.white;

            float scale = Mathf.Max(0.01f, backgroundTileScale);
            float viewH = _camera.orthographicSize * 2f;
            float viewW = viewH * _camera.aspect;
            const float edgeMargin = 0.2f;
            _backgroundRenderer.size = new Vector2(
                (viewW + edgeMargin) / scale,
                (viewH + edgeMargin) / scale);
            _backgroundRenderer.transform.position = new Vector3(
                _camera.transform.position.x,
                _camera.transform.position.y,
                0f);
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

            _skipStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
            };
            _skipStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
        }

        void OnGUI()
        {
            if (_titleStyle == null) InitStyles(); // GUIStyle 只能在 OnGUI 里用 GUI.skin 创建

            if (_isPlayingOpening)
            {
                DrawOpening();
                return;
            }

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;
            float bw = 240f;
            float bh = 60f;

            // 标题
            GUI.Label(new Rect(0f, cy - 170f, Screen.width, 80f), "《土豆发芽了！》", _titleStyle);

            // 开始
            if (GUI.Button(new Rect(cx - bw / 2f, cy - 30f, bw, bh), "开 始", _buttonStyle))
                BeginOpening();

            // 退出
            if (GUI.Button(new Rect(cx - bw / 2f, cy + 50f, bw, bh), "退 出", _buttonStyle))
                Quit();
        }

        void BeginOpening()
        {
            if (openingImageNames == null || openingImageNames.Length < 4)
            {
                Debug.LogWarning("MenuController：开场漫画需要按顺序配置 4 张图片，当前将直接进入第一关。");
                FinishOpening();
                return;
            }

            _isPlayingOpening = true;
            _openingImageStartTime = Time.unscaledTime;
        }

        void DrawOpening()
        {
            float elapsed = Time.unscaledTime - _openingImageStartTime;
            float enter = Mathf.Max(0.01f, openingPanelEnterDuration);
            float interval = Mathf.Max(0f, openingPanelInterval);
            float fourthStart = enter * 3f + interval * 2f;
            float fourthDuration = Mathf.Max(0.01f, openingFourthEnterDuration);
            float shiftStart = fourthStart + fourthDuration;
            float shiftDuration = Mathf.Max(0.01f, openingGroupShiftDuration);
            float shiftT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - shiftStart) / shiftDuration));

            const float comicAspect = 2.5f;
            float comicW = Mathf.Min(Screen.width * 0.94f, Screen.height * 0.78f * comicAspect);
            float comicH = comicW / comicAspect;
            var comicRect = new Rect(
                (Screen.width - comicW) / 2f,
                (Screen.height - comicH) / 2f,
                comicW,
                comicH);

            float groupW = comicRect.width * 0.426f;
            float imageScale = Mathf.Max(0.0001f, openingImageScale);
            float imageW = groupW * imageScale;
            float imageH = comicRect.height * imageScale;
            float groupShift = Screen.width * openingGroupShiftScreenRatio * shiftT;
            var sharedRect = new Rect(
                (Screen.width - imageW) / 2f + Screen.width * openingHorizontalOffsetScreenRatio - groupShift,
                (Screen.height - imageH) / 2f,
                imageW,
                imageH);

            for (int i = 0; i < 3; i++)
            {
                float panelStart = i * (enter + interval);
                if (elapsed < panelStart) continue;

                float panelT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - panelStart) / enter));
                Rect rect = sharedRect;
                float enterDistance = Screen.width * openingFirstThreeEnterScreenRatio;
                rect.x = Mathf.Lerp(rect.x + enterDistance, rect.x, panelT);
                DrawOpeningImage(i, rect);
            }

            if (elapsed >= fourthStart)
            {
                float fourthT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - fourthStart) / fourthDuration));
                Rect fourthRect = sharedRect;
                fourthRect.x = Mathf.Lerp(Screen.width + 20f, fourthRect.x, fourthT);
                DrawOpeningImage(3, fourthRect);
            }

            GUI.Label(new Rect(Screen.width - 180f, Screen.height - 42f, 160f, 24f), "Esc 跳过", _skipStyle);
        }

        void DrawOpeningImage(int index, Rect rect)
        {
            var sprite = SpriteLibrary.Get(openingImageNames[index], Color.white);
            if (sprite != null && sprite.texture != null)
                GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, true);
        }

        void FinishOpening()
        {
            _isPlayingOpening = false;
            if (!string.IsNullOrEmpty(firstLevelSceneName))
                SceneManager.LoadScene(firstLevelSceneName);
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
