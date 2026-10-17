using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GamersWeek
{
    // Health for the player or enemies. Things that hurt (enemies, hazards...) call TakeDamage.
    // The player gets one of these automatically, but add it yourself if you want to change the numbers.
    [AddComponentMenu("Gamers Week/Health")]
    public class Health : MonoBehaviour
    {
        [Tooltip("Health at the start.")]
        public int maxHealth = 100;
        [Tooltip("Seconds of invincibility after getting hit, so one touch doesn't take all your health.")]
        public float invincibleTime = 1f;
        [Tooltip("Flash red when hit (not used on the player - the HUD flashes instead).")]
        public bool flashWhenHit = true;

        [Header("When it dies (not the player - the player respawns)")]
        [Tooltip("Remove this object when health reaches 0.")]
        public bool destroyOnDeath = true;
        [Tooltip("Points added to the score when this dies.")]
        public int scoreOnDeath = 0;

        [Header("Events")]
        public UnityEvent onHurt = new UnityEvent();
        public UnityEvent onDeath = new UnityEvent();

        public int Current { get; private set; }
        public float Percent => maxHealth > 0 ? (float)Current / maxHealth : 0f;
        public bool IsDead => Current <= 0;
        public bool IsInvincible => Time.time < invincibleUntil;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        float invincibleUntil;
        bool IsPlayer => CompareTag("Player");

        void Awake() => Current = maxHealth;

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || IsInvincible) return;
            invincibleUntil = Time.time + invincibleTime;
            Current = Mathf.Max(0, Current - amount);
            onHurt.Invoke();
            if (Current == 0)
            {
                Die();
                return;
            }
            if (IsPlayer) KitSfx.Play(Sfx.Hurt);
            else if (flashWhenHit) StartCoroutine(FlashRed());
        }

        public void Heal(int amount)
        {
            if (!IsDead) Current = Mathf.Min(maxHealth, Current + amount);
        }

        public void Kill()
        {
            invincibleUntil = 0f;
            TakeDamage(Current);
        }

        // Back to full health, with a moment of invincibility (used when the player respawns).
        public void ResetHealth()
        {
            Current = maxHealth;
            invincibleUntil = Time.time + invincibleTime;
        }

        void Die()
        {
            onDeath.Invoke();
            if (IsPlayer)
            {
                GameManager.Instance.PlayerDied();
                return;
            }
            if (scoreOnDeath != 0) GameManager.Instance.AddScore(scoreOnDeath);
            if (destroyOnDeath) StartCoroutine(SquashAndDestroy());
        }

        IEnumerator FlashRed()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, Color.red);
            foreach (var r in renderers) r.SetPropertyBlock(block);
            yield return new WaitForSeconds(0.12f);
            foreach (var r in renderers)
                if (r != null) r.SetPropertyBlock(null);
        }

        IEnumerator SquashAndDestroy()
        {
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Vector3 startScale = transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
            {
                transform.localScale = new Vector3(startScale.x * (1f + t * 0.4f), startScale.y * (1f - t * 0.9f), startScale.z * (1f + t * 0.4f));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
