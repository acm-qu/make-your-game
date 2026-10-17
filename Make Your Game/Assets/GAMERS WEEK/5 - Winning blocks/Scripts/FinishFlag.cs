using UnityEngine;

namespace GamersWeek
{
    // Reach the flag to win the level.
    [AddComponentMenu("Gamers Week/Finish Flag")]
    public class FinishFlag : MonoBehaviour
    {
        [Tooltip("The player must collect the Game Manager's 'Coins Needed' before the flag counts.")]
        public bool needsCoins = false;
        [Tooltip("The part that waves.")]
        public Transform flag;

        void Update()
        {
            if (flag != null) flag.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 3f) * 15f, 0f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!GWPlayer.Is(other)) return;
            var game = GameManager.Instance;
            if (needsCoins && game.Coins < game.CoinsNeeded)
            {
                game.ShowMessage("Collect more coins first! (" + game.Coins + " / " + game.CoinsNeeded + ")");
                return;
            }
            game.Win();
        }
    }
}
