using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>Multiple-choice riddle UI: question, four options (click or keys 1-4), progress line. Esc closes.</summary>
    public class RiddlePanel : MonoBehaviour
    {
        public static RiddlePanel I { get; private set; }

        [SerializeField] bool built;
        [SerializeField] GameObject root;
        [SerializeField] Text header, question, progress;
        [SerializeField] Button[] options = new Button[4];

        Action<int> onAnswer;
        bool open;
        public bool IsOpen => open;

        void Awake()
        {
            I = this;
            if (!built || root == null) Build();
            for (int i = 0; i < options.Length; i++)
            {
                int k = i;
                UIFactory.OnClick(options[i], () => Choose(k));
            }
            root.SetActive(false);
        }

        public void Build()
        {
            UIFactory.Clear(transform);
            var canvas = UIFactory.CreateCanvas("RiddleCanvas", 40, transform);
            root = canvas.gameObject;
            var dim = UIFactory.Image(canvas.transform, "Dim", new Color(0, 0, 0, 0.45f)); UIFactory.Stretch(dim.rectTransform);
            var panel = UIFactory.Panel(canvas.transform, "RiddlePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 760)).rectTransform;
            header = UIFactory.Label(panel, "RIDDLE OF THE ANCIENTS", 40, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, 320), new Vector2(1000, 60));
            progress = UIFactory.Label(panel, "", 24, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal, new Vector2(0, 270), new Vector2(1000, 34));
            question = UIFactory.Label(panel, "", 34, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Italic, new Vector2(0, 160), new Vector2(980, 170));
            for (int i = 0; i < 4; i++)
                options[i] = UIFactory.Button(panel, "Option", new Vector2(0, 20 - i * 92), new Vector2(820, 76), 30);
            UIFactory.Label(panel, "Choose wisely: press 1-4 or click.  A wrong answer wakes the scorpion.   [Esc] step back", 20,
                UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal, new Vector2(0, -345), new Vector2(1060, 30));
            built = true;
        }

        public void Open(int number, int total, string text, string[] answers, Action<int> callback)
        {
            onAnswer = callback;
            progress.text = $"Riddle {number} of {total}";
            question.text = text;
            string[] letters = { "1", "2", "3", "4" };
            for (int i = 0; i < 4; i++) UIFactory.SetButtonLabel(options[i], $"{letters[i]}.   {answers[i]}");
            if (!open) UIState.PushModal();
            open = true;
            root.SetActive(true);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            root.SetActive(false);
            UIState.PopModal();
        }

        void Choose(int i)
        {
            if (!open) return;
            var cb = onAnswer;
            Close();
            cb?.Invoke(i);
        }

        void Update()
        {
            if (!open) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Choose(2);
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) Choose(3);
            else if (kb.escapeKey.wasPressedThisFrame) { UIState.EscConsumedFrame = Time.frameCount; Close(); }
        }
    }
}
