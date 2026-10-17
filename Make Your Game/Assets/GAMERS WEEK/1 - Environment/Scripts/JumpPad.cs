using UnityEngine;

namespace GamersWeek
{
    // Step on it to get thrown into the air.
    [AddComponentMenu("Gamers Week/Jump Pad")]
    public class JumpPad : MonoBehaviour
    {
        [Tooltip("How high it throws you, in meters.")]
        public float jumpHeight = 6f;
        [Tooltip("Also throw boxes and other physics objects.")]
        public bool launchObjects = true;
        public bool playSound = true;
        [Tooltip("The part that squishes when it bounces.")]
        public Transform padTop;

        float readyTime;
        float squash;
        Vector3 padScale;

        void Start()
        {
            if (padTop != null) padScale = padTop.localScale;
        }

        void Update()
        {
            if (padTop == null || squash <= 0f) return;
            squash = Mathf.Max(0f, squash - Time.deltaTime * 4f);
            float stretch = 1f + Mathf.Sin(squash * Mathf.PI) * 2f;
            padTop.localScale = new Vector3(padScale.x, padScale.y * stretch, padScale.z);
        }

        void OnTriggerStay(Collider other)
        {
            if (Time.time < readyTime) return;
            if (GWPlayer.Is(other))
            {
                if (GWPlayer.VerticalVelocity > 1f) return; // already flying up
                GWPlayer.LaunchToHeight(jumpHeight);
            }
            else if (launchObjects && other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)
            {
                var rb = other.attachedRigidbody;
                var velocity = rb.linearVelocity;
                velocity.y = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y);
                rb.linearVelocity = velocity;
            }
            else
            {
                return;
            }
            readyTime = Time.time + 0.3f;
            squash = 1f;
            if (playSound) KitSfx.Play(Sfx.Bounce);
        }
    }
}
