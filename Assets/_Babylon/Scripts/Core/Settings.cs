using System;
using UnityEngine;

namespace Babylon
{
    /// <summary>Player preferences stored in PlayerPrefs; applied instantly via the Changed event.</summary>
    public static class Settings
    {
        public static event Action Changed;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat("bab_music", 0.6f);
            set { PlayerPrefs.SetFloat("bab_music", Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat("bab_sfx", 0.85f);
            set { PlayerPrefs.SetFloat("bab_sfx", Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        /// <summary>Degrees of rotation per mouse pixel.</summary>
        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat("bab_sens", 0.12f);
            set { PlayerPrefs.SetFloat("bab_sens", Mathf.Clamp(value, 0.02f, 0.5f)); Changed?.Invoke(); }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt("bab_full", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("bab_full", value ? 1 : 0);
                Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Changed?.Invoke();
            }
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
