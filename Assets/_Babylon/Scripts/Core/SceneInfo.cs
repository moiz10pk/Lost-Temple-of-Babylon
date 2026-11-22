using System.Collections;
using UnityEngine;

namespace Babylon
{
    /// <summary>Per-scene setup: title card, music, ambience, opening objective and story line.</summary>
    public class SceneInfo : MonoBehaviour
    {
        public static SceneInfo Current { get; private set; }

        public int sceneIndex;
        public string title;
        [TextArea] public string caption;
        public MusicId music = MusicId.Calm;
        public SoundId ambient = SoundId.None;
        public float ambientVolume = 0.5f;
        public string objective;
        [TextArea] public string openingStory;
        [Tooltip("Gives the sword when this scene is played directly from the editor (boss onwards).")]
        public bool grantSword;
        public bool updateEnvironmentLighting;

        void Awake()
        {
            Current = this;
            GameManager.Ensure();
            if (grantSword) GameManager.I.HasSword = true;
        }

        IEnumerator Start()
        {
            if (updateEnvironmentLighting) DynamicGI.UpdateEnvironment();
            if (AudioManager.I)
            {
                AudioManager.I.PlayMusic(music);
                AudioManager.I.PlayAmbient(ambient, ambientVolume);
            }
            if (SceneLoader.I) SceneLoader.I.BeginScene(title, caption);
            if (HUD.I && !string.IsNullOrEmpty(objective)) HUD.I.SetObjective(objective);
            if (!string.IsNullOrEmpty(openingStory))
            {
                yield return new WaitForSeconds(4.5f);
                if (HUD.I) HUD.I.ShowSubtitle(openingStory, 7f);
            }
        }
    }
}
