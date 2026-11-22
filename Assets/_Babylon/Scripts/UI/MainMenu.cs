using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>Title screen: Start Game (story intro card), Settings, Quit.</summary>
    public class MainMenu : MonoBehaviour
    {
        [TextArea(3, 6)]
        public string story = "Babylon, 1750 BC.\n\nLegends speak of a Sumerian tablet hidden beneath the great temple, guarded by traps, riddles and a guardian of living stone.\n\nBlind spiders hunt its halls by sound alone. Tread softly.\n\nYou have come to claim it.";

        [SerializeField] bool built;
        [SerializeField] GameObject menuRoot, storyRoot;
        [SerializeField] Button start, settings, quit;
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] Text storyText;

        bool starting;

        void Awake()
        {
            if (!built || menuRoot == null) Build();
            UIFactory.EnsureEventSystem();
            UIFactory.OnClick(start, () => StartCoroutine(StartRoutine()));
            UIFactory.OnClick(settings, () => { menuRoot.SetActive(false); settingsPanel.Open(); });
            UIFactory.OnClick(quit, PauseMenu.QuitGame);
            settingsPanel.Closed += () => menuRoot.SetActive(true);
            storyRoot.SetActive(false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        public void Build()
        {
            UIFactory.Clear(transform);
            var canvas = UIFactory.CreateCanvas("MenuCanvas", 20, transform);
            var rt = (RectTransform)canvas.transform;
            var shade = UIFactory.Image(rt, "LeftShade", new Color(0, 0, 0, 0.45f));
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = new Vector2(0.48f, 1);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

            menuRoot = UIFactory.Rect("Menu", rt, new Vector2(0, 0.5f), new Vector2(140, 60), new Vector2(800, 700)).gameObject;
            var m = (RectTransform)menuRoot.transform;
            var t1 = UIFactory.Label(m, "THE LOST TEMPLE", 92, UIFactory.TitleGold, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 250), new Vector2(900, 110));
            var t2 = UIFactory.Label(m, "OF BABYLON", 92, UIFactory.TitleGold, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 150), new Vector2(900, 110));
            UIFactory.AddShadow(t1); UIFactory.AddShadow(t2);
            UIFactory.Label(m, "An adventure beneath the ziggurat", 28, UIFactory.TextColor, TextAnchor.MiddleLeft, FontStyle.Italic, new Vector2(0, 80), new Vector2(800, 40));
            start = UIFactory.Button(m, "Start Game", new Vector2(-230, -30), new Vector2(340, 72), 32);
            settings = UIFactory.Button(m, "Settings", new Vector2(-230, -120), new Vector2(340, 72), 32);
            quit = UIFactory.Button(m, "Quit", new Vector2(-230, -210), new Vector2(340, 72), 32);
            foreach (var b in new[] { start, settings, quit })
            {
                var r = (RectTransform)b.transform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            }

            var cap = UIFactory.Label(rt, "Begin your adventure", 34, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic);
            cap.rectTransform.anchorMin = cap.rectTransform.anchorMax = new Vector2(0.5f, 0); cap.rectTransform.anchoredPosition = new Vector2(0, 60);
            cap.rectTransform.sizeDelta = new Vector2(900, 50);
            UIFactory.AddShadow(cap);

            settingsPanel = SettingsPanel.Create(rt);

            var storyImg = UIFactory.Image(rt, "Story", new Color(0.03f, 0.02f, 0.01f, 0.96f));
            UIFactory.Stretch(storyImg.rectTransform);
            storyRoot = storyImg.gameObject;
            storyText = UIFactory.Label(storyImg.rectTransform, "", 40, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic, Vector2.zero, new Vector2(1300, 600));
            UIFactory.Label(storyImg.rectTransform, "[ click to continue ]", 22, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal, new Vector2(0, -420), new Vector2(600, 30));
            built = true;
        }

        IEnumerator StartRoutine()
        {
            if (starting) yield break;
            starting = true;
            GameManager.I.ResetRun();
            storyRoot.SetActive(true);
            storyText.text = "";
            float t = 0;
            bool skip = false;
            // Typewriter reveal, click to skip; then continue after 6 s or a click.
            for (int i = 0; i <= story.Length && !skip; i++)
            {
                storyText.text = story.Substring(0, i);
                yield return new WaitForSecondsRealtime(0.025f);
                skip = Clicked();
            }
            storyText.text = story;
            yield return null;
            while (t < 6f && !Clicked()) { t += Time.unscaledDeltaTime; yield return null; }
            SceneLoader.I.Load(GameManager.SceneNames[1]);
        }

        static bool Clicked()
        {
            var m = Mouse.current; var k = Keyboard.current;
            return (m != null && m.leftButton.wasPressedThisFrame) || (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame));
        }
    }
}
