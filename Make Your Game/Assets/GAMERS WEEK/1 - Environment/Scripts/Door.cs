using UnityEngine;
using UnityEngine.Events;

namespace GamersWeek
{
    // A door that swings (or slides) open. It can be opened by a Button, by walking up to it, or by walking up with enough coins.
    [AddComponentMenu("Gamers Week/Door")]
    public class Door : MonoBehaviour, IActivatable
    {
        public enum OpenWhen { ButtonPressed, PlayerWalksUp, PlayerHasEnoughCoins }

        [Tooltip("Button Pressed: put this door in a Button's Targets.\nPlayer Walks Up: opens by itself.\nPlayer Has Enough Coins: opens when the player walks up with enough coins.")]
        public OpenWhen openWhen = OpenWhen.ButtonPressed;
        [Tooltip("Coins needed for 'Player Has Enough Coins'.")]
        public int coinsNeeded = 5;
        [Tooltip("How close the player has to be for the walk-up options.")]
        public float openDistance = 3.5f;
        [Tooltip("Close again when the Button is released or the player walks away.")]
        public bool closesAgain = false;

        [Header("Movement")]
        [Tooltip("The door model that swings open (the LowPolyDoor inside this prefab). Change its swing angle and speed on its Low Poly Door component.")]
        public LowPolyDoor swingingDoor;
        [Tooltip("Used when there's no Swinging Door: this part slides by Open Offset instead.")]
        public Transform panel;
        [Tooltip("How the panel slides when it opens.")]
        public Vector3 openOffset = new Vector3(0f, 3.1f, 0f);
        public float openSpeed = 4f;

        [Header("Events")]
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();

        public bool IsOpen { get; private set; }

        Vector3 closedPosition;
        float nextHintTime;

        void Start()
        {
            if (swingingDoor != null) return;
            if (panel == null) panel = transform;
            closedPosition = panel.localPosition;
        }

        void Update()
        {
            if (openWhen != OpenWhen.ButtonPressed) CheckPlayer();
            if (swingingDoor != null) return; // it animates itself
            Vector3 target = closedPosition + (IsOpen ? openOffset : Vector3.zero);
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, openSpeed * Time.deltaTime);
        }

        void CheckPlayer()
        {
            var player = GWPlayer.Get();
            bool near = player != null && Vector3.Distance(player.transform.position, transform.position) < openDistance;
            if (near && !IsOpen)
            {
                var game = GameManager.Instance;
                if (openWhen == OpenWhen.PlayerHasEnoughCoins && game.Coins < coinsNeeded)
                {
                    if (Time.time > nextHintTime) game.ShowMessage("This door needs " + coinsNeeded + " coins (" + game.Coins + " / " + coinsNeeded + ")");
                    nextHintTime = Time.time + 3f; // hint again only after the player has walked away for a bit
                    return;
                }
                Open();
            }
            else if (!near && IsOpen && closesAgain)
            {
                Close();
            }
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (swingingDoor != null) swingingDoor.Open();
            KitSfx.Play(Sfx.Door);
            onOpened.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (swingingDoor != null) swingingDoor.Close();
            KitSfx.Play(Sfx.Door);
            onClosed.Invoke();
        }

        public void Activate() => Open();

        public void Deactivate()
        {
            if (closesAgain) Close();
        }
    }
}
