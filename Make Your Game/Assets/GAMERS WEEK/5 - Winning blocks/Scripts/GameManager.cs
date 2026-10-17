using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GamersWeek
{
    // The "brain" of a level: counts coins and score, keeps track of lives and checkpoints,
    // and shows the win / game over screens. Put one in every level.
    // (Forgot? One gets created automatically the first time something needs it.)
    [AddComponentMenu("Gamers Week/Game Manager")]
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        [Header("Lives")]
        [Tooltip("How many times the player can die before it's Game Over. 0 = unlimited lives.")]
        public int lives = 3;

        [Header("Coins")]
        [Tooltip("Win as soon as enough coins are collected (no Finish Flag needed).")]
        public bool winByCollectingCoins = false;
        [Tooltip("How many coins count as 'enough'. 0 = every coin in the level.")]
        public int coinsNeeded = 0;

        [Header("Screens")]
        public string winTitle = "YOU WIN!";
        public Color winColor = new Color(0.3f, 0.9f, 0.4f);
        public string loseTitle = "GAME OVER";
        public Color loseColor = new Color(1f, 0.3f, 0.3f);

        [Header("Events")]
        public UnityEvent onWin = new UnityEvent();
        public UnityEvent onLose = new UnityEvent();

        public int Score { get; private set; }
        public int Coins { get; private set; }
        public int CoinsInLevel { get; private set; }
        public int CoinsNeeded => coinsNeeded > 0 ? coinsNeeded : CoinsInLevel;
        public int LivesLeft { get; private set; }
        public bool IsGameOver { get; private set; }

        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity;
        GameObject winScreen, loseScreen;
        Text winDetails, loseDetails, messageText;
        float hideMessageTime;

        static GameManager instance;
        static bool quitting;

        public static GameManager Instance
        {
            get
            {
                if (instance == null && !quitting)
                {
                    instance = FindAnyObjectByType<GameManager>();
                    if (instance == null)
                    {
                        Debug.Log("No GameManager in the scene, so one was made automatically. Drag in the GameManager prefab to change its settings.");
                        instance = new GameObject("GameManager (auto)").AddComponent<GameManager>();
                    }
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            quitting = false;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("There are two GameManagers in this scene. Removing the extra one.", this);
                Destroy(gameObject);
                return;
            }
            instance = this;
            Time.timeScale = 1f;
            LivesLeft = lives;
            BuildScreens();
        }

        void Start()
        {
            var player = GWPlayer.Get();
            if (player == null) return;
            spawnPosition = player.transform.position;
            spawnRotation = player.transform.rotation;
            _ = GWPlayer.Health; // gives the player Health if they don't have it, so enemies can hurt them
            GWPlayer.RestoreCursor();
        }

        void Update()
        {
            if (messageText.gameObject.activeSelf && Time.unscaledTime > hideMessageTime)
                messageText.gameObject.SetActive(false);
        }

        void OnApplicationQuit() => quitting = true;

        // ---- Coins & score ----

        public void RegisterCoin() => CoinsInLevel++;

        public void CollectCoin(int points)
        {
            Coins++;
            AddScore(points);
            if (winByCollectingCoins && Coins >= CoinsNeeded) Win();
        }

        public void AddScore(int points) => Score += points;

        // ---- Player ----

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        // Called when the player's health hits 0 or they fall into a Kill Zone.
        public void PlayerDied()
        {
            if (IsGameOver) return;
            if (lives > 0)
            {
                LivesLeft--;
                if (LivesLeft <= 0)
                {
                    Lose();
                    return;
                }
            }
            KitSfx.Play(Sfx.Hurt);
            RespawnPlayer();
            if (lives > 0) ShowMessage(LivesLeft == 1 ? "Last life!" : LivesLeft + " lives left");
        }

        // Sends the player back to the last checkpoint (or the start) with full health.
        public void RespawnPlayer()
        {
            GWPlayer.Teleport(spawnPosition, spawnRotation);
            var health = GWPlayer.Health;
            if (health != null) health.ResetHealth();
        }

        // ---- Winning & losing ----

        public void Win()
        {
            if (IsGameOver) return;
            winDetails.text = "Score: " + Score + "     Coins: " + Coins;
            EndGame(winScreen, Sfx.Win);
            onWin.Invoke();
        }

        public void Lose()
        {
            if (IsGameOver) return;
            loseDetails.text = "Score: " + Score;
            EndGame(loseScreen, Sfx.Lose);
            onLose.Invoke();
        }

        void EndGame(GameObject screen, Sfx sound)
        {
            IsGameOver = true;
            messageText.gameObject.SetActive(false);
            screen.SetActive(true);
            KitSfx.Play(sound);
            GWPlayer.SetControlsEnabled(false);
            KitUI.EnsureEventSystem();
            Time.timeScale = 0f;
        }

        public void Restart() => ReloadScene();

        public static void ReloadScene()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Works in the editor even if the scene isn't in the Build Settings list.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }

        // ---- Messages ----

        // Shows a line of text near the bottom of the screen for a few seconds.
        public void ShowMessage(string message, float seconds = 2.5f)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            hideMessageTime = Time.unscaledTime + seconds;
        }

        void BuildScreens()
        {
            var canvas = KitUI.MakeCanvas("Screens", transform, 100).transform;
            messageText = KitUI.AddText(KitUI.MakeRect("Message", canvas, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(1400, 80)), "", 48, Color.white);
            messageText.gameObject.SetActive(false);
            winScreen = BuildScreen(canvas, "Win Screen", winTitle, winColor, "Play Again", out winDetails);
            loseScreen = BuildScreen(canvas, "Lose Screen", loseTitle, loseColor, "Try Again", out loseDetails);
        }

        GameObject BuildScreen(Transform canvas, string name, string title, Color color, string buttonLabel, out Text details)
        {
            var center = new Vector2(0.5f, 0.5f);
            var screen = KitUI.MakeFill(name, canvas);
            KitUI.AddImage(screen, new Color(0f, 0f, 0f, 0.75f)).raycastTarget = true; // blocks clicks to anything behind
            KitUI.AddText(KitUI.MakeRect("Title", screen, center, new Vector2(0, 140), new Vector2(1600, 200)), title, 150, color);
            details = KitUI.AddText(KitUI.MakeRect("Details", screen, center, new Vector2(0, 0), new Vector2(1600, 80)), "", 48, Color.white);
            KitUI.MakeButton(screen, buttonLabel, new Vector2(0, -150), new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f), Restart);
            screen.gameObject.SetActive(false);
            return screen.gameObject;
        }
    }
}
