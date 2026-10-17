using UnityEngine;

namespace GamersWeek
{
    // Added to the player automatically the first time they get knocked back. You don't need to add it yourself.
    [AddComponentMenu("")]
    public class PlayerKnockback : MonoBehaviour
    {
        const float Slowdown = 8f;

        Vector3 velocity;
        CharacterController controller;

        public void Push(Vector3 offset) => velocity = offset * Slowdown;

        public void Stop() => velocity = Vector3.zero;

        void Update()
        {
            if (velocity.sqrMagnitude < 0.01f) return;
            Vector3 step = velocity * Time.deltaTime;
            if (controller == null) TryGetComponent(out controller);
            if (controller != null && controller.enabled) controller.Move(step);
            else transform.position += step;
            velocity = Vector3.Lerp(velocity, Vector3.zero, Slowdown * Time.deltaTime);
        }
    }
}
