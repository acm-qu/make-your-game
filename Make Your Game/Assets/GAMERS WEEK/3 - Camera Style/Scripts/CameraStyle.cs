using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;

namespace GamersWeek
{
    // Makes a Cinemachine Camera follow the player as soon as the game starts.
    // The camera with the highest Priority is the one you see, so these take over from the normal first person camera.
    // To go back to first person, switch this camera object off.
    [AddComponentMenu("Gamers Week/Camera Style")]
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraStyle : MonoBehaviour
    {
        [Tooltip("Follow the player's head, so the camera turns with the mouse. Off = follow the player's body.")]
        public bool followHead = true;
        [Tooltip("Keep the camera pointed at the player.")]
        public bool lookAtPlayer = false;
        [Tooltip("Stop the mouse from turning the player. WASD then moves up/down/left/right on the screen (good for top-down).")]
        public bool turnOffMouseLook = false;
        [Tooltip("Show the player's body. First person players often hide it, which would make them invisible from this camera.")]
        public bool showPlayerBody = true;

        void Start()
        {
            var player = GWPlayer.Get();
            if (player == null)
            {
                Debug.LogWarning("Camera Style: couldn't find anything tagged 'Player' to follow.", this);
                return;
            }

            var cam = GetComponent<CinemachineCamera>();
            Transform head = FindHead(player);
            cam.Follow = followHead && head != null ? head : player.transform;
            if (lookAtPlayer) cam.LookAt = player.transform;
            if (turnOffMouseLook) GWPlayer.SetMouseLook(false);
            if (showPlayerBody)
                foreach (var body in player.GetComponentsInChildren<MeshRenderer>(true)) body.enabled = true;

            // Cinemachine cameras only work if the Main Camera has a Cinemachine Brain.
            var main = Camera.main;
            if (main != null && !main.TryGetComponent<CinemachineBrain>(out _)) main.gameObject.AddComponent<CinemachineBrain>();
        }

        static Transform FindHead(GameObject player)
        {
            if (player.TryGetComponent(out FirstPersonController fps) && fps.CinemachineCameraTarget != null) return fps.CinemachineCameraTarget.transform;
            if (player.TryGetComponent(out ThirdPersonController tps) && tps.CinemachineCameraTarget != null) return tps.CinemachineCameraTarget.transform;
            return null;
        }
    }
}
