using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>In-game HUD: health/stamina/noise bars, objective, prompts, subtitles, boss bar, minimap, flashes.</summary>
    public class HUD : MonoBehaviour
    {
        public static HUD I { get; private set; }

        public Texture minimapTexture;

        [SerializeField] bool built;
        [SerializeField] RectTransform healthFill, staminaFill, noiseFill, bossFill;
        [SerializeField] GameObject bossRoot, minimapRoot, messageRoot, keyIcon, swordIcon;
        [SerializeField] Text bossName, objectiveText, promptText, subtitleText, hintText, messageTitle, messageBody, healthText;
        [SerializeField] RawImage minimapImage;
        [SerializeField] Image flash;
        [SerializeField] CanvasGroup subtitleGroup, hintGroup;

        float subtitleTimer, hintTimer, flashAlpha;
        Color flashColor = Color.red;
        int messageOpenedFrame;

        public bool MessageOpen => messageRoot && messageRoot.activeSelf;

        void Awake()
        {
            I = this;
            if (!built || healthFill == null) Build();
            UIFactory.EnsureEventSystem();
        }

        void Start()
        {
            SetMinimap(minimapTexture);
            ShowSword(GameManager.I && GameManager.I.HasSword);
        }

        public void Build()
        {
            UIFactory.Clear(transform);
            var canvas = GetComponent<Canvas>();
            if (!canvas)
            {
                canvas = gameObject.AddComponent<Canvas>();
                var sc = gameObject.AddComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
                gameObject.AddComponent<GraphicRaycaster>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var root = transform;

            flash = UIFactory.Image(root, "DamageFlash", new Color(1, 0, 0, 0)); UIFactory.Stretch(flash.rectTransform); flash.raycastTarget = false;

            // Vitals (bottom-left)
            var vit = UIFactory.Panel(root, "Vitals", new Vector2(0, 0), new Vector2(30, 30), new Vector2(430, 128)).rectTransform;
            UIFactory.Label(vit, "HEALTH", 18, UIFactory.TitleGold, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(-150, 38), new Vector2(110, 26));
            healthFill = UIFactory.Bar(vit, "Health", new Vector2(0.5f, 0.5f), new Vector2(40, 38), new Vector2(300, 24), new Color32(0xB3, 0x22, 0x1C, 255));
            healthText = UIFactory.Label(healthFill.parent, "100", 16, UIFactory.TextColor);
            UIFactory.Stretch(healthText.rectTransform);
            UIFactory.Label(vit, "STAMINA", 18, UIFactory.TitleGold, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(-150, 2), new Vector2(110, 26));
            staminaFill = UIFactory.Bar(vit, "Stamina", new Vector2(0.5f, 0.5f), new Vector2(40, 2), new Vector2(300, 16), new Color32(0xD4, 0xA5, 0x37, 255));
            UIFactory.Label(vit, "NOISE", 18, UIFactory.TitleGold, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(-150, -34), new Vector2(110, 26));
            noiseFill = UIFactory.Bar(vit, "Noise", new Vector2(0.5f, 0.5f), new Vector2(40, -34), new Vector2(300, 16), new Color32(0x3F, 0xD0, 0xFF, 255));

            // Objective (top-left)
            var obj = UIFactory.Panel(root, "Objective", new Vector2(0, 1), new Vector2(30, -30), new Vector2(560, 90)).rectTransform;
            UIFactory.Label(obj, "OBJECTIVE", 18, UIFactory.TitleGold, TextAnchor.UpperLeft, FontStyle.Bold, new Vector2(0, 26), new Vector2(520, 24));
            objectiveText = UIFactory.Label(obj, "", 24, UIFactory.TextColor, TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(0, -10), new Vector2(520, 50));
            keyIcon = MakeIcon(root, "KeyIcon", "Icon_Key", new Vector2(610, -40));
            swordIcon = MakeIcon(root, "SwordIcon", "Icon_Sword", new Vector2(610, -40));

            // Boss bar (top-centre)
            var boss = UIFactory.Rect("BossBar", root, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(820, 70));
            bossRoot = boss.gameObject;
            bossName = UIFactory.Label(boss, "THE STONE MASTER", 26, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, 20), new Vector2(820, 32));
            UIFactory.AddShadow(bossName);
            bossFill = UIFactory.Bar(boss, "BossHealth", new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(800, 26), new Color32(0xC0, 0x18, 0x18, 255));
            bossRoot.SetActive(false);

            // Minimap (top-right)
            var mm = UIFactory.Rect("Minimap", root, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(300, 300));
            minimapRoot = mm.gameObject;
            var mmBg = mm.gameObject.AddComponent<Image>(); mmBg.color = new Color(0, 0, 0, 0.7f);
            minimapImage = new GameObject("MapImage", typeof(RectTransform)).AddComponent<RawImage>();
            minimapImage.transform.SetParent(mm, false); UIFactory.Stretch(minimapImage.rectTransform);
            minimapImage.rectTransform.offsetMin = new Vector2(5, 5); minimapImage.rectTransform.offsetMax = new Vector2(-5, -5);
            UIFactory.AddBorder(mm, UIFactory.Gold, 4f);
            UIFactory.Label(mm, "MAP", 18, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, -165), new Vector2(200, 24));
            minimapRoot.SetActive(false);

            // Crosshair + prompt
            var cross = UIFactory.Image(root, "Crosshair", new Color(1, 0.95f, 0.85f, 0.7f));
            cross.rectTransform.sizeDelta = new Vector2(6, 6);
            promptText = UIFactory.Label(root, "", 30, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            promptText.rectTransform.anchoredPosition = new Vector2(0, -120); promptText.rectTransform.sizeDelta = new Vector2(900, 50);
            UIFactory.AddShadow(promptText);

            // Subtitle + hint
            subtitleText = UIFactory.Label(root, "", 28, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic);
            var srt = subtitleText.rectTransform; srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0); srt.anchoredPosition = new Vector2(0, 200); srt.sizeDelta = new Vector2(1300, 120);
            UIFactory.AddShadow(subtitleText);
            subtitleGroup = subtitleText.gameObject.AddComponent<CanvasGroup>(); subtitleGroup.alpha = 0;
            hintText = UIFactory.Label(root, "", 26, new Color32(0x3F, 0xD0, 0xFF, 255), TextAnchor.MiddleCenter, FontStyle.Bold);
            hintText.rectTransform.anchoredPosition = new Vector2(0, 190); hintText.rectTransform.sizeDelta = new Vector2(1200, 50);
            UIFactory.AddShadow(hintText);
            hintGroup = hintText.gameObject.AddComponent<CanvasGroup>(); hintGroup.alpha = 0;

            // Controls help (bottom-right)
            var help = UIFactory.Label(root, "WASD Move   Shift Sprint   Space Jump   C Crouch   Ctrl Duck   Q Roll\nLMB Strike (hold: Spin, in air: Plunge)   RMB Block   E Interact   Esc Pause",
                16, new Color(0.95f, 0.89f, 0.76f, 0.6f), TextAnchor.LowerRight);
            var hrt = help.rectTransform; hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(1, 0); hrt.anchoredPosition = new Vector2(-24, 20); hrt.sizeDelta = new Vector2(900, 50);

            // Message panel (clay tablet clue etc.)
            var msg = UIFactory.Panel(root, "Message", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 380)).rectTransform;
            messageRoot = msg.gameObject;
            messageTitle = UIFactory.Label(msg, "", 36, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, 140), new Vector2(840, 50));
            messageBody = UIFactory.Label(msg, "", 30, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic, new Vector2(0, 0), new Vector2(820, 220));
            UIFactory.Label(msg, "[ Press E to close ]", 20, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal, new Vector2(0, -160), new Vector2(600, 30));
            messageRoot.SetActive(false);

            keyIcon.SetActive(false); swordIcon.SetActive(false);
            built = true;
        }

        GameObject MakeIcon(Transform root, string name, string tex, Vector2 pos)
        {
            var rt = UIFactory.Rect(name, root, new Vector2(0, 1), pos, new Vector2(70, 70));
            var ri = rt.gameObject.AddComponent<RawImage>();
            var lib = BabylonLibrary.I;
            ri.texture = lib ? lib.Tex(tex) : null;
            ri.raycastTarget = false;
            return rt.gameObject;
        }

        void Update()
        {
            NoiseSystem.Decay(Time.unscaledDeltaTime);
            UIFactory.SetBar(noiseFill, NoiseSystem.PlayerLoudness);

            if (subtitleTimer > 0) { subtitleTimer -= Time.unscaledDeltaTime; subtitleGroup.alpha = Mathf.Clamp01(subtitleTimer / 0.6f); }
            if (hintTimer > 0) { hintTimer -= Time.unscaledDeltaTime; hintGroup.alpha = Mathf.Clamp01(hintTimer / 0.5f); }
            if (flashAlpha > 0)
            {
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0, Time.unscaledDeltaTime * 1.2f);
                flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
            }

            if (MessageOpen && Time.frameCount > messageOpenedFrame + 1)
            {
                var kb = Keyboard.current;
                if (kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                {
                    if (kb.escapeKey.wasPressedThisFrame) UIState.EscConsumedFrame = Time.frameCount;
                    HideMessage();
                }
            }
        }

        public void SetHealth(float n, float current)
        {
            UIFactory.SetBar(healthFill, n);
            if (healthText) healthText.text = Mathf.CeilToInt(current).ToString();
        }

        public void SetStamina(float n) => UIFactory.SetBar(staminaFill, n);
        public void SetObjective(string s) { if (objectiveText) objectiveText.text = s; }

        public void ShowPrompt(string s)
        {
            if (!promptText) return;
            promptText.text = s ?? "";
        }

        public void ShowSubtitle(string s, float seconds = 5f)
        {
            subtitleText.text = s; subtitleTimer = seconds; subtitleGroup.alpha = 1;
        }

        public void ShowHint(string s, float seconds = 2.5f)
        {
            hintText.text = s; hintTimer = seconds; hintGroup.alpha = 1;
        }

        public void Flash(Color c, float strength = 0.45f) { flashColor = c; flashAlpha = Mathf.Max(flashAlpha, strength); }

        public void ShowBoss(string name, bool show)
        {
            bossRoot.SetActive(show);
            if (show) bossName.text = name;
        }

        public void SetBoss(float n) => UIFactory.SetBar(bossFill, n);

        public void ShowKey(bool v) { keyIcon.SetActive(v); if (v) swordIcon.SetActive(false); }
        public void ShowSword(bool v) { swordIcon.SetActive(v); }

        public void SetMinimap(Texture t)
        {
            minimapImage.texture = t;
            minimapRoot.SetActive(t != null);
        }

        public void ShowMessage(string title, string body)
        {
            messageTitle.text = title; messageBody.text = body;
            if (!messageRoot.activeSelf) UIState.PushLock();
            messageRoot.SetActive(true);
            messageOpenedFrame = Time.frameCount;
        }

        public void HideMessage()
        {
            if (!messageRoot.activeSelf) return;
            messageRoot.SetActive(false);
            UIState.PopLock();
            Interactor.SuppressUntilFrame = Time.frameCount + 1;
        }
    }
}
