using System.Collections.Generic;
using UnityEngine;

namespace GamersWeek
{
    // A platform that travels between where you put it and its "End Point" child, carrying whatever stands on it.
    // Drag the End Point around in the Scene view to change where it goes (the yellow line shows the path).
    [AddComponentMenu("Gamers Week/Moving Platform")]
    [DefaultExecutionOrder(-50)] // moves before the player does, so riding it is smooth
    public class MovingPlatform : MonoBehaviour, IActivatable
    {
        [Tooltip("Where the platform travels to.")]
        public Transform endPoint;
        public float speed = 2f;
        [Tooltip("Seconds to wait at each end.")]
        public float waitAtEnds = 1f;
        [Tooltip("On: keeps going back and forth.\nOff: goes to the End Point when switched on and comes back when switched off (like a lift).")]
        public bool loop = true;
        [Tooltip("Start moving as soon as the game starts. Turn off if a Button should switch it on.")]
        public bool startOn = true;

        Vector3 startPosition, endPosition;
        bool moving, goingToEnd = true;
        float waitTimer;
        Collider body;
        readonly Collider[] hits = new Collider[16];
        readonly List<Object> riders = new List<Object>();

        void Start()
        {
            startPosition = transform.position;
            endPosition = endPoint != null ? endPoint.position : startPosition;
            body = FindBody();
            if (startOn) Activate();
        }

        public void Activate()
        {
            moving = true;
            if (!loop) goingToEnd = true;
        }

        public void Deactivate()
        {
            if (loop)
            {
                moving = false;
                return;
            }
            goingToEnd = false;
            moving = true;
        }

        void Update()
        {
            if (!moving) return;
            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                return;
            }

            Vector3 goal = goingToEnd ? endPosition : startPosition;
            MoveBy(Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime) - transform.position);
            if (transform.position != goal) return;

            if (loop)
            {
                goingToEnd = !goingToEnd;
                waitTimer = waitAtEnds;
            }
            else
            {
                moving = false;
            }
        }

        void MoveBy(Vector3 delta)
        {
            FindRiders();
            // Going up: lift the riders first so the platform doesn't push into them. Going down: the reverse.
            if (delta.y > 0f)
            {
                MoveRiders(delta);
                transform.position += delta;
                Physics.SyncTransforms();
            }
            else
            {
                transform.position += delta;
                Physics.SyncTransforms();
                MoveRiders(delta);
            }
        }

        void FindRiders()
        {
            riders.Clear();
            if (body == null) return;
            Bounds bounds = body.bounds;
            var center = new Vector3(bounds.center.x, bounds.max.y + 0.15f, bounds.center.z);
            var halfSize = new Vector3(bounds.extents.x, 0.25f, bounds.extents.z);
            int count = Physics.OverlapBoxNonAlloc(center, halfSize, hits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform)) continue;
                Object rider = null;
                if (hit is CharacterController) rider = hit;
                else if (hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic) rider = hit.attachedRigidbody;
                if (rider != null && !riders.Contains(rider)) riders.Add(rider);
            }
        }

        void MoveRiders(Vector3 delta)
        {
            foreach (var rider in riders)
            {
                if (rider is CharacterController controller)
                {
                    if (controller.enabled) controller.Move(delta);
                }
                else if (rider is Rigidbody rb)
                {
                    rb.position += delta;
                }
            }
        }

        Collider FindBody()
        {
            foreach (var c in GetComponentsInChildren<Collider>())
                if (!c.isTrigger) return c;
            return null;
        }

        void OnDrawGizmos()
        {
            if (endPoint == null) return;
            Vector3 from = Application.isPlaying ? startPosition : transform.position;
            Vector3 to = Application.isPlaying ? endPosition : endPoint.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(from, to);
            var c = Application.isPlaying ? body : FindBody();
            if (c != null) Gizmos.DrawWireCube(to + (c.bounds.center - transform.position), c.bounds.size);
        }
    }
}
