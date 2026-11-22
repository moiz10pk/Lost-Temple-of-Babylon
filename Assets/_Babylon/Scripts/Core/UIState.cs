using UnityEngine;

namespace Babylon
{
    /// <summary>Global input/cursor gate. Modals free the cursor; locks freeze the player (cutscenes).</summary>
    public static class UIState
    {
        public static int Modals;
        public static int Locks;
        public static bool Paused;
        public static bool GameOver;
        /// <summary>Frame on which a panel already used the Esc key (so the pause menu ignores it).</summary>
        public static int EscConsumedFrame = -1;

        public static bool CursorFree => Modals > 0 || Paused || GameOver;
        public static bool InputBlocked => Modals > 0 || Locks > 0 || Paused || GameOver;

        public static void PushModal() => Modals++;
        public static void PopModal() => Modals = Mathf.Max(0, Modals - 1);
        public static void PushLock() => Locks++;
        public static void PopLock() => Locks = Mathf.Max(0, Locks - 1);

        public static void ResetAll()
        {
            Modals = 0; Locks = 0; Paused = false; GameOver = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void DomainReset() { Modals = 0; Locks = 0; Paused = false; GameOver = false; }
    }
}
