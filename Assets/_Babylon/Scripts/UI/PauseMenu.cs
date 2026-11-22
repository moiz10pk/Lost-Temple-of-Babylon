using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>Esc menu: Resume / Settings / Main Menu / Quit. Freezes time and frees the cursor.</summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu I { get; private set; }

        [SerializeField] bool built;
        [SerializeField] GameObject root, mainPanel;
        [SerializeField] Button resume, settings, menu, quit;
        [SerializeField] SettingsPanel settingsPanel;

        void Awake()
        {
            I = this;
            if (!built || root == null) Build();
            UIFactory.OnClick(resume, Resume);
            UIFactory.OnClick(settings, () => { mainPanel.SetActive(false); settingsPanel.Open(); });
            UIFactory.OnClick(menu, () => { Resume(); SceneLoader.I.Load(GameManager.SceneNames[0]); });
            UIFactory.OnClick(quit, QuitGame);
            settingsPanel.Closed += () => mainPanel.SetActive(true);
            root.SetActive(false);
            UIFactory.EnsureEventSystem();
        }

        public void Build()
        {
            UIFactory.Clear(transform);
            var canvas = UIFactory.CreateCanvas("PauseCanvas", 50, transform);
            var dim = UIFactory.Image(canvas.transform, "Dim", new Color(0, 0, 0, 0.6f)); UIFactory.Stretch(dim.rectTransform);
            root = canvas.gameObject;
            var panel = UIFactory.Panel(canvas.transform, "PausePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 520));
            mainPanel = panel.gameObject;
            var rt = panel.rectTransform;
            UIFactory.Label(rt, "PAUSED", 52, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, 190), new Vector2(480, 70));
            resume = UIFactory.Button(rt, "Resume", new Vector2(0, 80), new Vector2(340, 66));
            settings = UIFactory.Button(rt, "Settings", new Vector2(0, -5), new Vector2(340, 66));
            menu = UIFactory.Button(rt, "Main Menu", new Vector2(0, -90), new Vector2(340, 66));
            quit = UIFactory.Button(rt, "Quit", new Vector2(0, -175), new Vector2(340, 66));
            settingsPanel = SettingsPanel.Create(canvas.transform);
            built = true;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (UIState.GameOver || (SceneLoader.I && SceneLoader.I.IsLoading)) return;
            if (UIState.EscConsumedFrame == Time.frameCount) return;
            if (HUD.I && HUD.I.MessageOpen) return;
            if (!UIState.Paused && UIState.Modals > 0) return; // riddle panel handles its own Esc
            if (UIState.Paused) Resume(); else Pause();
        }

        public void Pause()
        {
            UIState.Paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            root.SetActive(true); mainPanel.SetActive(true); settingsPanel.gameObject.SetActive(false);
        }

        public void Resume()
        {
            UIState.Paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            root.SetActive(false);
        }

        public static void QuitGame()
        {
            Settings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
