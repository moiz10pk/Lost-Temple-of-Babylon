using System;
using UnityEngine;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>Music / SFX / sensitivity / fullscreen panel shared by the main menu and pause menu.</summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] Slider music, sfx, sens;
        [SerializeField] Toggle full;
        [SerializeField] Button back;

        public event Action Closed;

        public static SettingsPanel Create(Transform parent)
        {
            var img = UIFactory.Panel(parent, "SettingsPanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 520));
            var p = img.gameObject.AddComponent<SettingsPanel>();
            var rt = img.rectTransform;
            UIFactory.Label(rt, "SETTINGS", 44, UIFactory.TitleGold, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0, 200), new Vector2(600, 60));
            p.music = UIFactory.Slider(rt, "Music volume", new Vector2(0, 110), Settings.MusicVolume);
            p.sfx = UIFactory.Slider(rt, "Sound effects volume", new Vector2(0, 40), Settings.SfxVolume);
            p.sens = UIFactory.Slider(rt, "Mouse sensitivity", new Vector2(0, -30), Mathf.InverseLerp(0.02f, 0.5f, Settings.MouseSensitivity));
            p.full = UIFactory.Toggle(rt, "Fullscreen", new Vector2(0, -100), Settings.Fullscreen);
            p.back = UIFactory.Button(rt, "Back", new Vector2(0, -190), new Vector2(260, 64));
            img.gameObject.SetActive(false);
            return p;
        }

        bool wired;

        // Listeners are added at runtime (they are not serialized when the panel lives in a prefab).
        void Wire()
        {
            if (wired || music == null) return;
            wired = true;
            music.onValueChanged.AddListener(v => Settings.MusicVolume = v);
            sfx.onValueChanged.AddListener(v => { Settings.SfxVolume = v; });
            sens.onValueChanged.AddListener(v => Settings.MouseSensitivity = Mathf.Lerp(0.02f, 0.5f, v));
            full.onValueChanged.AddListener(v => Settings.Fullscreen = v);
            UIFactory.OnClick(back, Close);
        }

        void OnEnable()
        {
            if (music == null) return;
            Wire();
            music.SetValueWithoutNotify(Settings.MusicVolume);
            sfx.SetValueWithoutNotify(Settings.SfxVolume);
            sens.SetValueWithoutNotify(Mathf.InverseLerp(0.02f, 0.5f, Settings.MouseSensitivity));
            full.SetIsOnWithoutNotify(Settings.Fullscreen);
        }

        public void Open() => gameObject.SetActive(true);

        public void Close()
        {
            Settings.Save();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
