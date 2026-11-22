using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>
    /// Round 2 flow. Three riddles in a row must be answered correctly. A wrong answer summons the scorpion,
    /// which stings the player (25 damage) and the round starts again. Three correct answers raise the sword pedestal.
    /// </summary>
    public class RiddleMaster : MonoBehaviour
    {
        public RuneWheel wheel;
        public Brazier[] braziers;
        public SwordPedestal pedestal;
        public StoneDoor exitDoor;
        public int riddlesToSolve = 3;

        readonly List<int> order = new List<int>();
        int progress;
        bool busy, solved;

        public bool Solved => solved;

        void Start() { Shuffle(); Refresh(); }

        void Shuffle()
        {
            order.Clear();
            for (int i = 0; i < RiddleData.Pool.Length; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (order[i], order[j]) = (order[j], order[i]); }
        }

        void Refresh()
        {
            if (wheel) wheel.SetLit(progress);
            for (int i = 0; i < braziers.Length; i++) braziers[i].SetLit(i < progress, false);
        }

        public bool CanAsk => !busy && !solved;

        public void Ask()
        {
            if (!CanAsk || !RiddlePanel.I) return;
            var r = RiddleData.Pool[order[progress]];
            RiddlePanel.I.Open(progress + 1, riddlesToSolve, r.text, r.options, OnAnswer);
        }

        void OnAnswer(int choice)
        {
            var r = RiddleData.Pool[order[progress]];
            if (choice == r.answer) StartCoroutine(Correct());
            else StartCoroutine(Wrong(r.options[choice]));
        }

        IEnumerator Correct()
        {
            busy = true;
            progress++;
            AudioManager.I?.Play2D(SoundId.Chime, 1f);
            if (wheel) wheel.SetLit(progress);
            if (progress - 1 < braziers.Length) braziers[progress - 1].SetLit(true);
            if (progress >= riddlesToSolve)
            {
                solved = true;
                yield return Complete();
                yield break;
            }
            if (HUD.I) HUD.I.ShowHint($"Correct! {riddlesToSolve - progress} riddle(s) remain.", 2f);
            yield return new WaitForSeconds(1.4f);
            busy = false;
            Ask();
        }

        IEnumerator Wrong(string chosen)
        {
            busy = true;
            UIState.PushLock();
            AudioManager.I?.Play2D(SoundId.WrongBuzz, 1f);
            if (HUD.I) HUD.I.ShowSubtitle($"\"{chosen}\"... WRONG. The floor cracks open beneath the tablet!", 3f);
            var player = PlayerVitals.I;
            var prefab = BabylonLibrary.I ? BabylonLibrary.I.Prefab("Scorpion") : null;
            if (prefab && player)
            {
                var s = Instantiate(prefab).GetComponent<Scorpion>();
                yield return s.Strike(player, null);
            }
            else if (player) player.Health.TakeDamage(new DamageInfo(25, DamageKind.Sting, player.transform.position, Vector3.forward));
            UIState.PopLock();

            progress = 0;
            Shuffle();
            Refresh();
            if (player && !player.IsDead && HUD.I)
                HUD.I.ShowSubtitle("The scorpion's venom burns. The trial of riddles begins anew - all three must be answered again.", 5f);
            yield return new WaitForSeconds(1f);
            busy = false;
        }

        IEnumerator Complete()
        {
            if (HUD.I) HUD.I.ShowSubtitle("The rune wheel turns... the ancients judge you worthy.", 4f);
            if (wheel) wheel.Spin();
            AudioManager.I?.Play2D(SoundId.VictoryFanfare, 0.6f);
            yield return new WaitForSeconds(1.5f);
            if (pedestal) pedestal.Rise();
            if (HUD.I) HUD.I.SetObjective("Take the Sword of Marduk from the pedestal");
            busy = false;
        }

        /// <summary>Called by the pedestal once the sword is taken.</summary>
        public void OnSwordTaken()
        {
            if (exitDoor) exitDoor.Open();
            if (HUD.I)
            {
                HUD.I.SetObjective("Enter the Guardian's chamber");
                HUD.I.ShowSubtitle("With the Sword of Marduk in hand, the great door grinds open. Something vast sleeps beyond.", 5f);
            }
        }
    }
}
