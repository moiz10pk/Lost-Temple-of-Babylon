using UnityEngine;
using UnityEngine.SceneManagement;

namespace Babylon
{
    /// <summary>Persistent run state: play time, deaths, sword. Creates itself so any scene can be played directly.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public float PlayTime;
        public int Deaths;
        public bool HasSword;
        public bool RunFinished;

        public static readonly string[] SceneNames =
            { "00_MainMenu", "01_Temple", "02_Maze", "03_Riddles", "04_Boss", "05_FinalChamber" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() => Ensure();

        public static GameManager Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("GameManager");
            return go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            if (!GetComponent<AudioManager>()) gameObject.AddComponent<AudioManager>();
            if (!GetComponent<SceneLoader>()) gameObject.AddComponent<SceneLoader>();
            Screen.fullScreenMode = Settings.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        void Update()
        {
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx >= 1 && !RunFinished && !UIState.Paused) PlayTime += Time.deltaTime;
        }

        public void ResetRun()
        {
            PlayTime = 0; Deaths = 0; HasSword = false; RunFinished = false;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
