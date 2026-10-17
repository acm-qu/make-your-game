using System.Collections;
using UnityEngine;

namespace GamersWeek
{
    // A spinning collectible. Adds to the Game Manager's coin count and score when the player touches it.
    [AddComponentMenu("Gamers Week/Coin")]
    public class Coin : MonoBehaviour
    {
        [Tooltip("Points added to the score.")]
        public int points = 10;
        public float spinSpeed = 120f;
        [Tooltip("How far it floats up and down.")]
        public float bobHeight = 0.15f;
        public bool playSound = true;
        [Tooltip("The part that spins and bobs.")]
        public Transform model;

        Vector3 modelStart;
        bool collected;

        void Start()
        {
            if (model == null) model = transform;
            modelStart = model.localPosition;
            GameManager.Instance.RegisterCoin();
        }

        void Update()
        {
            if (collected) return;
            model.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            // Offset by position so a row of coins doesn't bob in perfect sync.
            float bob = Mathf.Sin(Time.time * 2.5f + transform.position.x + transform.position.z) * bobHeight;
            model.localPosition = modelStart + Vector3.up * bob;
        }

        void OnTriggerEnter(Collider other)
        {
            if (collected || !GWPlayer.Is(other)) return;
            collected = true;
            GameManager.Instance.CollectCoin(points);
            if (playSound) KitSfx.Play(Sfx.Coin);
            StartCoroutine(PopAndDisappear());
        }

        IEnumerator PopAndDisappear()
        {
            Vector3 startScale = model.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
            {
                model.localScale = startScale * (1f + t * 0.6f) * (1f - t);
                model.localPosition += Vector3.up * (4f * Time.deltaTime);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
