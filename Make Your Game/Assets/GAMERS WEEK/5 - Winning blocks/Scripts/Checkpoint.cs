using UnityEngine;

namespace GamersWeek
{
    // Touch it and you'll respawn here instead of at the start. Respawning faces the way the checkpoint faces (blue arrow).
    [AddComponentMenu("Gamers Week/Checkpoint")]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Parts that light up when the checkpoint is reached.")]
        public Renderer[] lightParts;
        public Color offColor = new Color(0.5f, 0.5f, 0.55f);
        public Color onColor = new Color(0.2f, 1f, 0.4f);
        public bool showMessage = true;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static Checkpoint current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => current = null;

        void Start() => SetLit(false);

        void OnTriggerEnter(Collider other)
        {
            if (current == this || !GWPlayer.Is(other)) return;
            if (current != null) current.SetLit(false);
            current = this;
            SetLit(true);
            GameManager.Instance.SetCheckpoint(transform.position + Vector3.up * 0.1f, transform.rotation);
            KitSfx.Play(Sfx.Checkpoint);
            if (showMessage) GameManager.Instance.ShowMessage("Checkpoint!");
        }

        void SetLit(bool lit)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, lit ? onColor : offColor);
            block.SetColor(EmissionColor, lit ? onColor * 2f : Color.black);
            foreach (var part in lightParts)
                if (part != null) part.SetPropertyBlock(block);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position + Vector3.up * 0.2f;
            Gizmos.DrawLine(start, start + transform.forward * 1.5f);
        }
    }
}
