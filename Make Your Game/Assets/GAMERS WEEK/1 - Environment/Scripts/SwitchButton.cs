using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GamersWeek
{
    // Switches other things on and off: Doors, Moving Platforms, or anything you hook up in the events.
    // Drag what it should control into "Targets". A blue line in the Scene view shows what it's connected to.
    [AddComponentMenu("Gamers Week/Button")]
    public class SwitchButton : MonoBehaviour
    {
        public enum PressMode { StepOnIt, PressKeyNearby }

        [Tooltip("Step On It: a pressure plate.\nPress Key Nearby: walk up and press a key.")]
        public PressMode pressMode = PressMode.StepOnIt;
        [Tooltip("Things to switch on. Drag Doors, Moving Platforms etc. here from the Hierarchy.")]
        public GameObject[] targets;
        [Tooltip("Stays on forever once pressed.\nOff: a pressure plate pops back up when nothing is on it, and a key button flips on/off with each press.")]
        public bool staysPressed = false;

        [Header("Step On It")]
        [Tooltip("Only the player can press it. Off = boxes and other physics objects can too.")]
        public bool playerOnly = false;

        [Header("Press Key Nearby")]
        public Key key = Key.E;
        [Tooltip("How close the player needs to be.")]
        public float reach = 2.5f;
        [Tooltip("The 'Press E' sign that shows when the player is close.")]
        public GameObject prompt;

        [Header("Look")]
        [Tooltip("The part that moves down when pressed and changes color.")]
        public Renderer cap;
        public Color offColor = new Color(1f, 0.25f, 0.2f);
        public Color onColor = new Color(0.2f, 1f, 0.35f);

        [Header("Events")]
        public UnityEvent onPressed = new UnityEvent();
        public UnityEvent onReleased = new UnityEvent();

        public bool IsPressed { get; private set; }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        readonly Collider[] hits = new Collider[16];
        Vector3 capUpPosition, plateCenter, plateHalfSize;

        void Start()
        {
            if (cap != null)
            {
                capUpPosition = cap.transform.localPosition;
                Bounds bounds = cap.bounds;
                plateCenter = transform.InverseTransformPoint(bounds.center + Vector3.up * 0.35f);
                plateHalfSize = new Vector3(bounds.extents.x, 0.4f, bounds.extents.z);
            }
            if (prompt != null)
            {
                prompt.SetActive(false);
                var label = prompt.GetComponentInChildren<UnityEngine.UI.Text>();
                if (label != null) label.text = "Press " + key;
            }
            SetColor(offColor);
        }

        void Update()
        {
            bool pressed = IsPressed;
            if (pressMode == PressMode.StepOnIt)
            {
                if (SomethingOnPlate()) pressed = true;
                else if (!staysPressed) pressed = false;
            }
            else
            {
                var player = GWPlayer.Get();
                bool near = player != null && Vector3.Distance(GWPlayer.Center, transform.position) < reach;
                if (prompt != null) prompt.SetActive(near && !(IsPressed && staysPressed));
                if (near && KeyPressed() && !(IsPressed && staysPressed)) pressed = !IsPressed;
            }
            if (pressed != IsPressed) SetPressed(pressed);

            if (cap != null)
            {
                Vector3 target = capUpPosition - (IsPressed ? Vector3.up * 0.05f : Vector3.zero);
                cap.transform.localPosition = Vector3.MoveTowards(cap.transform.localPosition, target, Time.deltaTime);
            }
        }

        void LateUpdate()
        {
            // Turn the sign to face the camera.
            if (prompt != null && prompt.activeSelf && Camera.main != null)
                prompt.transform.rotation = Quaternion.LookRotation(prompt.transform.position - Camera.main.transform.position);
        }

        bool KeyPressed()
        {
            if (key != Key.None && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame) return true;
            return Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
        }

        bool SomethingOnPlate()
        {
            if (cap == null) return false;
            int count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(plateCenter), plateHalfSize, hits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform)) continue;
                if (GWPlayer.Is(hit)) return true;
                if (!playerOnly && hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic) return true;
            }
            return false;
        }

        void SetPressed(bool pressed)
        {
            IsPressed = pressed;
            KitSfx.Play(Sfx.Click);
            SetColor(pressed ? onColor : offColor);
            foreach (var target in targets)
            {
                if (target == null) continue;
                var switchables = target.GetComponents<IActivatable>();
                if (switchables.Length == 0) switchables = target.GetComponentsInParent<IActivatable>();
                foreach (var s in switchables)
                {
                    if (pressed) s.Activate();
                    else s.Deactivate();
                }
            }
            if (pressed) onPressed.Invoke();
            else onReleased.Invoke();
        }

        void SetColor(Color color)
        {
            if (cap == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            block.SetColor(EmissionColor, color * 1.5f);
            cap.SetPropertyBlock(block);
        }

        void OnDrawGizmos()
        {
            if (targets == null) return;
            Gizmos.color = new Color(0.3f, 0.7f, 1f);
            foreach (var target in targets)
                if (target != null) Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, target.transform.position);
        }
    }
}
