using UnityEngine;

namespace GamersWeek
{
    // An invisible box (red in the Scene view). Put it under your level: falling into it sends the player
    // back to the last checkpoint. Resize it with the Box Collider.
    [AddComponentMenu("Gamers Week/Kill Zone")]
    [RequireComponent(typeof(BoxCollider))]
    public class KillZone : MonoBehaviour
    {
        [Tooltip("Falling in costs a life. Off = just go back to the last checkpoint.")]
        public bool costsALife = true;
        [Tooltip("Also delete other things that fall in (boxes, enemies...).")]
        public bool destroyOtherThings = true;

        void OnTriggerEnter(Collider other)
        {
            if (GWPlayer.Is(other))
            {
                if (costsALife) GameManager.Instance.PlayerDied();
                else GameManager.Instance.RespawnPlayer();
                return;
            }
            if (!destroyOtherThings) return;
            var thing = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (!thing.CompareTag("Player")) Destroy(thing);
        }

        void OnDrawGizmos()
        {
            if (!TryGetComponent(out BoxCollider box)) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0f, 0f, 0.12f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
