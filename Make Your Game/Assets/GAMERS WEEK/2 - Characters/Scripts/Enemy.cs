using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace GamersWeek
{
    // An enemy that walks between patrol points and/or chases the player, and hurts them on touch.
    // Jump on its head to defeat it!
    // No NavMesh baked? No problem: one is built automatically when the game starts.
    [AddComponentMenu("Gamers Week/Enemy")]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : MonoBehaviour
    {
        [Header("Walking")]
        [Tooltip("Points to walk between, in order. Move the 'Patrol Point' children around in the Scene view, or duplicate them to add more.")]
        public Transform[] patrolPoints;
        public float walkSpeed = 2.5f;
        [Tooltip("Seconds to wait at each patrol point.")]
        public float waitAtPoint = 1f;

        [Header("Chasing")]
        [Tooltip("Starts chasing the player when they get this close. 0 = never chases.")]
        public float chaseRange = 0f;
        [Tooltip("Gives up when the player gets this far away.")]
        public float giveUpRange = 15f;
        public float chaseSpeed = 4.5f;
        [Tooltip("Only start chasing if it can see the player (not through walls).")]
        public bool needsToSeePlayer = true;
        [Tooltip("Shown above its head while chasing (optional).")]
        public GameObject alertIcon;

        [Header("Attacking")]
        [Tooltip("Damage done to the player on touch.")]
        public int damage = 25;
        [Tooltip("How far the player gets pushed back when hit.")]
        public float knockback = 2.5f;
        [Tooltip("Can the player defeat it by jumping on its head?")]
        public bool canBeStomped = true;

        NavMeshAgent agent;
        Health health;
        Vector3[] route;
        int routeIndex;
        float waitTimer;
        Vector3 home;
        bool chasing;
        float height = 1.6f;
        float radius = 0.5f;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            if (TryGetComponent(out CapsuleCollider capsule))
            {
                height = capsule.height * transform.lossyScale.y;
                radius = capsule.radius * transform.lossyScale.x;
            }

            // Remember the patrol points now, because they're children and would move with us.
            route = new Vector3[patrolPoints.Length];
            for (int i = 0; i < route.Length; i++)
                route[i] = patrolPoints[i] != null ? patrolPoints[i].position : transform.position;
            if (alertIcon != null) alertIcon.SetActive(false);

            AutoNavMesh.Ensure(transform.position);
            if (!NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
            {
                Debug.LogWarning(name + " isn't standing on any walkable ground, so it can't move.", this);
                enabled = false;
                return;
            }
            home = hit.position;
            agent.enabled = true;
            agent.Warp(hit.position);
            agent.speed = walkSpeed;
            if (route.Length > 0) agent.SetDestination(route[0]);
        }

        void Update()
        {
            if (health != null && health.IsDead)
            {
                agent.enabled = false;
                return;
            }
            var player = GWPlayer.Get();
            UpdateChasing(player);
            if (chasing)
            {
                agent.speed = chaseSpeed;
                agent.SetDestination(player.transform.position);
            }
            else
            {
                Patrol();
            }
            if (player != null) TouchPlayer(player);
        }

        void Patrol()
        {
            agent.speed = walkSpeed;
            if (route.Length == 0 || agent.pathPending || agent.remainingDistance > 0.3f) return;
            waitTimer += Time.deltaTime;
            if (waitTimer < waitAtPoint) return;
            waitTimer = 0f;
            routeIndex = (routeIndex + 1) % route.Length;
            agent.SetDestination(route[routeIndex]);
        }

        void UpdateChasing(GameObject player)
        {
            bool wasChasing = chasing;
            if (player == null || chaseRange <= 0f)
            {
                chasing = false;
            }
            else
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                chasing = chasing ? distance < giveUpRange : distance < chaseRange && (!needsToSeePlayer || CanSeePlayer());
            }
            if (chasing == wasChasing) return;

            if (alertIcon != null) alertIcon.SetActive(chasing);
            if (chasing) KitSfx.Play(Sfx.Alert, 0.25f);
            else agent.SetDestination(route.Length > 0 ? route[routeIndex] : home);
        }

        bool CanSeePlayer()
        {
            Vector3 eyes = transform.position + Vector3.up * (height * 0.8f);
            if (!Physics.Linecast(eyes, GWPlayer.Center, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return true;
            return GWPlayer.Is(hit.collider);
        }

        void TouchPlayer(GameObject player)
        {
            Vector3 offset = player.transform.position - transform.position;
            float sideways = new Vector2(offset.x, offset.z).magnitude;
            bool touching = sideways < radius + 0.7f && offset.y > -1f && offset.y < height + 0.3f;
            if (!touching) return;

            // Landing on its head defeats it.
            if (canBeStomped && offset.y > height * 0.6f && GWPlayer.VerticalVelocity < 0.5f)
            {
                GWPlayer.LaunchToHeight(2f);
                KitSfx.Play(Sfx.Stomp);
                if (health != null) health.Kill();
                else Destroy(gameObject);
                return;
            }

            var playerHealth = GWPlayer.Health;
            if (playerHealth == null || playerHealth.IsInvincible || playerHealth.IsDead) return;
            GWPlayer.Knockback(transform.position, knockback);
            playerHealth.TakeDamage(damage);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            int count = Application.isPlaying && route != null ? route.Length : patrolPoints != null ? patrolPoints.Length : 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 from = PointPosition(i);
                Vector3 to = PointPosition((i + 1) % count);
                Gizmos.DrawSphere(from, 0.2f);
                Gizmos.DrawLine(from, to);
            }
        }

        Vector3 PointPosition(int i)
        {
            if (Application.isPlaying && route != null) return route[i];
            return patrolPoints[i] != null ? patrolPoints[i].position : transform.position;
        }

        void OnDrawGizmosSelected()
        {
            if (chaseRange <= 0f) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
    }

    // Builds a NavMesh for the whole level when the game starts, if nobody baked one.
    static class AutoNavMesh
    {
        const int IgnoreRaycastLayer = 2; // the kit puts its trigger zones on this layer

        static NavMeshSurface surface;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => surface = null;

        public static void Ensure(Vector3 near)
        {
            if (surface != null || NavMesh.SamplePosition(near, out _, 4f, NavMesh.AllAreas)) return;

            // Don't let the player's own body become a bump in the walkable area.
            var player = GWPlayer.Get();
            if (player != null && !player.TryGetComponent<NavMeshModifier>(out _))
                player.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            surface = new GameObject("NavMesh (auto-built)").AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(1 << IgnoreRaycastLayer);
            surface.BuildNavMesh();
        }
    }
}
