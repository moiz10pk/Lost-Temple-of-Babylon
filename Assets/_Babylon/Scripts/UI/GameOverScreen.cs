using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>"You have fallen" with Retry (reload scene) and Main Menu. Counts deaths.</summary>
    public class GameOverScreen : MonoBehaviour
    {
        public static GameOverScreen I { get; private set; }

        [SerializeField] bool built;
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] Text reason;
        [SerializeField] Button retry, menu;

        void Awake()
        {
            I = this;
            if (!built || root == null) Build();
            UIFactory.OnClick(retry, () => SceneLoader.I.Reload());
            UIFactory.OnClick(menu, () => SceneLoader.I.Load(GameManager.SceneNames[0]));
            root.SetActive(false);
        }

        public void Build()
        {
            UIFactory.Clear(transform);
            var canvas = UIFactory.CreateCanvas("GameOverCanvas", 60, transform);
            root = canvas.gameObject;
            group = root.AddComponent<CanvasGroup>();
            var dim = UIFactory.Image(canvas.transform, "Dim", new Color(0.12f, 0, 0, 0.75f)); UIFactory.Stretch(dim.rectTransform);
            var rt = (RectTransform)canvas.transform;
            var title = UIFactory.Label(rt, "YOU HAVE FALLEN", 84, new Color32(0xC0, 0x22, 0x18, 255), TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, 150), new Vector2(1400, 120));
            UIFactory.AddShadow(title);
            reason = UIFactory.Label(rt, "", 30, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic, new Vector2(0, 50), new Vector2(1200, 80));
            retry = UIFactory.Button(rt, "Retry", new Vector2(0, -70), new Vector2(340, 70));
            menu = UIFactory.Button(rt, "Main Menu", new Vector2(0, -160), new Vector2(340, 70));
            built = true;
        }

        public void Show(string why)
        {
            if (UIState.GameOver) return;
            UIState.GameOver = true;
            if (GameManager.I) GameManager.I.Deaths++;
            reason.text = why;
            StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn()
        {
            yield return new WaitForSeconds(1.4f);
            root.SetActive(true);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.8f) { group.alpha = t; yield return null; }
            group.alpha = 1;
        }
    }
}
