using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Babylon.Tests
{
    /// <summary>Loads every scene and runs it for a while; any logged error or exception fails the test.</summary>
    public class SceneSmokeTests
    {
        static readonly string[] Scenes = { "00_MainMenu", "01_Temple", "02_Maze", "03_Riddles", "04_Boss", "05_FinalChamber" };

        [UnityTest]
        public IEnumerator EveryScene_RunsWithoutErrors([ValueSource(nameof(Scenes))] string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            Time.timeScale = 2f;
            float t = 0;
            while (t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator Boss_WakesAndFights_WithoutErrors()
        {
            SceneManager.LoadScene("04_Boss");
            yield return null;
            yield return null;
            Teleport(new Vector3(0, 0.1f, -2f));
            Time.timeScale = 3f;
            float t = 0;
            while (t < 14f) { t += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
            Assert.IsNotNull(GameObject.Find("StoneMaster"));
        }

        [UnityTest]
        public IEnumerator Maze_SpiderHunts_WithoutErrors()
        {
            SceneManager.LoadScene("02_Maze");
            yield return null;
            yield return null;
            var spider = GameObject.Find("Spider");
            Assert.IsNotNull(spider, "No spider in maze");
            Teleport(spider.transform.position + spider.transform.forward * 1.2f + Vector3.up * 0.1f);
            Time.timeScale = 2f;
            float t = 0;
            while (t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
        }

        static void Teleport(Vector3 p)
        {
            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player missing");
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false; player.transform.position = p; cc.enabled = true;
        }
    }
}
