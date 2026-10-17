using UnityEngine;
using UnityEngine.UI;

namespace GamersWeek
{
    // Shows the player's health, lives, coins and score.
    // It's all normal UI: move things around, recolor them, or delete the parts you don't need.
    [AddComponentMenu("Gamers Week/HUD")]
    public class HUD : MonoBehaviour
    {
        [Header("Parts (any of these can be left empty)")]
        public RectTransform healthFill;
        public Text healthText;
        public Text livesText;
        public Text coinsText;
        public Text scoreText;
        [Tooltip("Flashes red when the player gets hurt.")]
        public Image hurtFlash;

        Health playerHealth;
        int lastHealth = -1;
        float flash;

        void Update()
        {
            var game = GameManager.Instance;
            if (playerHealth == null) playerHealth = GWPlayer.Health;

            if (playerHealth != null)
            {
                if (healthFill != null) healthFill.anchorMax = new Vector2(playerHealth.Percent, 1f);
                if (healthText != null) healthText.text = playerHealth.Current + " / " + playerHealth.maxHealth;
                if (playerHealth.Current < lastHealth) flash = 1f;
                lastHealth = playerHealth.Current;
            }
            if (livesText != null) livesText.text = game.lives > 0 ? "Lives: " + game.LivesLeft : "";
            if (coinsText != null) coinsText.text = game.CoinsNeeded > 0 ? "Coins: " + game.Coins + " / " + game.CoinsNeeded : "Coins: " + game.Coins;
            if (scoreText != null) scoreText.text = "Score: " + game.Score;
            if (hurtFlash != null)
            {
                flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.5f);
                hurtFlash.color = new Color(1f, 0f, 0f, flash * 0.35f);
            }
        }
    }
}
