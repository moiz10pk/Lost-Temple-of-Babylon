using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>Fade-to-black scene loads plus the gold title card shown after each load.</summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader I { get; private set; }

        Image fade;
        CanvasGroup cardGroup;
        Text cardTitle, cardCaption;
        bool loading;
        Coroutine cardRoutine;

        public bool IsLoading => loading;

        void Awake()
        {
            I = this;
            var canvas = UIFactory.CreateCanvas("FadeCanvas", 1000, transform);
            fade = UIFactory.Image(canvas.transform, "Fade", Color.black);
            UIFactory.Stretch(fade.rectTransform);
            fade.raycastTarget = false;

            var card = UIFactory.Rect("TitleCard", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1500, 300));
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0; cardGroup.blocksRaycasts = false;
            cardTitle = UIFactory.Label(card, "", 64, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold);
            cardTitle.rectTransform.anchoredPosition = new Vector2(0, 50); cardTitle.rectTransform.sizeDelta = new Vector2(1500, 110);
            UIFactory.AddShadow(cardTitle);
            cardCaption = UIFactory.Label(card, "", 30, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic);
            cardCaption.rectTransform.anchoredPosition = new Vector2(0, -40); cardCaption.rectTransform.sizeDelta = new Vector2(1400, 90);
            UIFactory.AddShadow(cardCaption);
        }

        public void Load(string sceneName)
        {
            if (loading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void Reload() => Load(SceneManager.GetActiveScene().name);

        public void LoadNext()
        {
            int i = SceneManager.GetActiveScene().buildIndex + 1;
            if (i < GameManager.SceneNames.Length) Load(GameManager.SceneNames[i]);
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            fade.raycastTarget = true;
            if (AudioManager.I) AudioManager.I.Play2D(SoundId.UIClick, 0.4f);
            yield return FadeTo(1f, 0.8f);
            UIState.ResetAll();
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            loading = false;
        }

        /// <summary>Called by SceneInfo when a scene starts: fade in and show its title card.</summary>
        public void BeginScene(string title, string caption)
        {
            StopAllCoroutines();
            loading = false;
            fade.color = Color.black;
            StartCoroutine(FadeInRoutine());
            if (!string.IsNullOrEmpty(title)) ShowTitleCard(title, caption);
        }

        IEnumerator FadeInRoutine()
        {
            yield return null;
            yield return FadeTo(0f, 1.2f);
            fade.raycastTarget = false;
        }

        public void ShowTitleCard(string title, string caption, float hold = 3f)
        {
            if (cardRoutine != null) StopCoroutine(cardRoutine);
            cardRoutine = StartCoroutine(CardRoutine(title, caption, hold));
        }

        IEnumerator CardRoutine(string title, string caption, float hold)
        {
            cardTitle.text = title; cardCaption.text = caption;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.8f) { cardGroup.alpha = t; yield return null; }
            cardGroup.alpha = 1;
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 1; t > 0; t -= Time.unscaledDeltaTime / 1.2f) { cardGroup.alpha = t; yield return null; }
            cardGroup.alpha = 0;
        }

        public IEnumerator FadeTo(float target, float duration)
        {
            Color c = fade.color;
            float start = c.a;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration))
            {
                c.a = Mathf.Lerp(start, target, t); fade.color = c; yield return null;
            }
            c.a = target; fade.color = c;
        }

        /// <summary>Fades to a colour without loading (used by the victory sequence).</summary>
        public void FlashFade(float target, float duration) => StartCoroutine(FadeTo(target, duration));
    }
}
