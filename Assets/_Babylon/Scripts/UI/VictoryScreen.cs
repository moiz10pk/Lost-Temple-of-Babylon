using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>End screen: "You have claimed the Hidden Sumerian Tablet", time, deaths, Main Menu / Quit.</summary>
    public class VictoryScreen : MonoBehaviour
    {
        public static VictoryScreen I { get; private set; }

        [SerializeField] bool built;
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] Text stats;
        [SerializeField] Button menu, quit;

        void Awake()
        {
            I = this;
            if (!built || root == null) Build();
            UIFactory.OnClick(menu, () => SceneLoader.I.Load(GameManager.SceneNames[0]));
            UIFactory.OnClick(quit, PauseMenu.QuitGame);
            root.SetActive(false);
        }

        public void Build()
        {
            UIFactory.Clear(transform);
            var canvas = UIFactory.CreateCanvas("VictoryCanvas", 70, transform);
            root = canvas.gameObject;
            group = root.AddComponent<CanvasGroup>();
            var bg = UIFactory.Image(canvas.transform, "Bg", new Color(0.05f, 0.03f, 0.01f, 0.92f)); UIFactory.Stretch(bg.rectTransform);
            var rt = (RectTransform)canvas.transform;
            var t = UIFactory.Label(rt, "You have claimed the\nHidden Sumerian Tablet", 72, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, 250), new Vector2(1600, 200));
            UIFactory.AddShadow(t);
            UIFactory.Label(rt, "The Stone Master has crumbled. The words of the first scribes are yours,\nand the secrets of Babylon will be spoken again.", 30,
                UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic, new Vector2(0, 90), new Vector2(1500, 100));
            stats = UIFactory.Label(rt, "", 36, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Normal, new Vector2(0, -20), new Vector2(1000, 100));
            menu = UIFactory.Button(rt, "Main Menu", new Vector2(0, -150), new Vector2(340, 70));
            quit = UIFactory.Button(rt, "Quit", new Vector2(0, -240), new Vector2(340, 70));
            built = true;
        }

        public void Show()
        {
            var gm = GameManager.I;
            gm.RunFinished = true;
            stats.text = $"Total time:  {GameManager.FormatTime(gm.PlayTime)}\nDeaths:  {gm.Deaths}";
            UIState.PushModal();
            root.SetActive(true);
            StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn()
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 1.5f) { group.alpha = t; yield return null; }
            group.alpha = 1;
        }
    }
}
