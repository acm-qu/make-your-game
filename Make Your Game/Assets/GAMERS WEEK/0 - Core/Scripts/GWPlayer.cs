using System.Reflection;
using StarterAssets;
using UnityEngine;

namespace GamersWeek
{
    // Everything the kit needs to do *to* the player lives here, so the other scripts stay simple.
    // "The player" is whatever object is tagged "Player". Works with the Starter Assets first and
    // third person controllers, and with Rigidbody players.
    public static class GWPlayer
    {
        // The Starter Assets controllers keep their vertical speed private, so we reach in to set it (for jump pads etc).
        const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly FieldInfo FirstPersonVelocity = typeof(FirstPersonController).GetField("_verticalVelocity", PrivateField);
        static readonly FieldInfo ThirdPersonVelocity = typeof(ThirdPersonController).GetField("_verticalVelocity", PrivateField);

        static GameObject player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => player = null;

        public static GameObject Get()
        {
            if (player == null) player = GameObject.FindGameObjectWithTag("Player");
            return player;
        }

        // True if this collider is part of the player (including colliders on the player's children).
        public static bool Is(Collider other)
        {
            var p = Get();
            if (p == null) return false;
            // Things the player is carrying have their own Rigidbody, so they don't count as the player.
            if (other.attachedRigidbody != null) return other.attachedRigidbody.gameObject == p;
            return other.transform.IsChildOf(p.transform);
        }

        // The player's Health. One is added automatically if the player doesn't have one yet.
        public static Health Health
        {
            get
            {
                var p = Get();
                if (p == null) return null;
                return p.TryGetComponent(out Health health) ? health : p.AddComponent<Health>();
            }
        }

        // Middle of the player's body (their feet are at transform.position).
        public static Vector3 Center
        {
            get
            {
                var p = Get();
                if (p == null) return Vector3.zero;
                return p.TryGetComponent(out CharacterController cc) ? cc.bounds.center : p.transform.position + Vector3.up;
            }
        }

        public static float VerticalVelocity
        {
            get
            {
                var p = Get();
                if (p == null) return 0f;
                if (p.TryGetComponent(out FirstPersonController fps)) return (float)FirstPersonVelocity.GetValue(fps);
                if (p.TryGetComponent(out ThirdPersonController tps)) return (float)ThirdPersonVelocity.GetValue(tps);
                if (p.TryGetComponent(out Rigidbody rb)) return rb.linearVelocity.y;
                return 0f;
            }
            set
            {
                var p = Get();
                if (p == null) return;
                if (p.TryGetComponent(out FirstPersonController fps)) FirstPersonVelocity.SetValue(fps, value);
                if (p.TryGetComponent(out ThirdPersonController tps)) ThirdPersonVelocity.SetValue(tps, value);
                if (p.TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
                {
                    var v = rb.linearVelocity;
                    v.y = value;
                    rb.linearVelocity = v;
                }
            }
        }

        // The player's gravity (a negative number).
        public static float Gravity
        {
            get
            {
                var p = Get();
                if (p != null && p.TryGetComponent(out FirstPersonController fps)) return fps.Gravity;
                if (p != null && p.TryGetComponent(out ThirdPersonController tps)) return tps.Gravity;
                return Physics.gravity.y;
            }
        }

        // Throws the player upwards so they reach about this many meters high.
        public static void LaunchToHeight(float meters)
        {
            VerticalVelocity = Mathf.Sqrt(2f * meters * Mathf.Max(0.01f, -Gravity));
        }

        // Pushes the player away from a point (used when an enemy hits them).
        public static void Knockback(Vector3 from, float distance)
        {
            var p = Get();
            if (p == null || distance <= 0f) return;
            Vector3 direction = p.transform.position - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = -p.transform.forward;
            if (!p.TryGetComponent(out PlayerKnockback knockback)) knockback = p.AddComponent<PlayerKnockback>();
            knockback.Push(direction.normalized * distance);
            LaunchToHeight(0.5f);
        }

        public static void Teleport(Vector3 position, Quaternion rotation)
        {
            var p = Get();
            if (p == null) return;
            // A CharacterController ignores position changes while it's on, so switch it off for a moment.
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            p.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            VerticalVelocity = 0f;
            if (p.TryGetComponent(out Rigidbody rb) && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
            if (p.TryGetComponent(out PlayerKnockback knockback)) knockback.Stop();
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }

        // Turns movement and mouse look on/off, and shows the mouse cursor while they're off (for menus).
        public static void SetControlsEnabled(bool enabled)
        {
            var p = Get();
            if (p != null)
            {
                if (p.TryGetComponent(out FirstPersonController fps)) fps.enabled = enabled;
                if (p.TryGetComponent(out ThirdPersonController tps)) tps.enabled = enabled;
                if (p.TryGetComponent(out StarterAssetsInputs input))
                {
                    input.cursorLocked = enabled;
                    input.move = Vector2.zero;
                    input.look = Vector2.zero;
                    input.jump = false;
                }
            }
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }

        // Lets the mouse turn the player/camera or not (top-down games usually don't want it).
        public static void SetMouseLook(bool enabled)
        {
            var p = Get();
            if (p == null || !p.TryGetComponent(out StarterAssetsInputs input)) return;
            input.cursorInputForLook = enabled;
            input.look = Vector2.zero;
        }

        // Locks the cursor again if the player's controller wants it locked (e.g. after restarting from a menu).
        public static void RestoreCursor()
        {
            var p = Get();
            if (p == null || !p.TryGetComponent(out StarterAssetsInputs input) || !input.cursorLocked) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
