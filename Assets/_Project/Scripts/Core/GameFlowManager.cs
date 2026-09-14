using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Estado")]
    [SerializeField] private GameState state = GameState.Countdown;

    [Header("Referências")]
    [SerializeField] private CountdownManager countdownManager;
    [SerializeField] private PlayerGridMovement2D playerMovement;
    [SerializeField] private EnemyGridMovement2D enemyMovement;
    [SerializeField] private PlayerPathRecorder2D playerPathRecorder;
    [SerializeField] private AIMemoryManager aiMemoryManager;

    [Header("UI")]
    [SerializeField] private GameObject deathCanvas;
    [SerializeField] private GameObject victoryCanvas;

    [Header("Morte")]
    [Min(0f)]
    [SerializeField] private float deathCanvasDelay = 0.75f;

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip victoryClip;

    [Header("Cenas")]
    [Min(0)]
    [SerializeField] private int mainMenuSceneIndex = 0;

    private bool sceneLoadRequested;

    public GameState State => state;
    public bool CanGameplayMove => state == GameState.Playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (deathCanvas != null)
            deathCanvas.SetActive(false);

        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);

        state = GameState.Countdown;

        if (countdownManager != null)
        {
            countdownManager.StartCountdown(this);
        }
        else
        {
            Debug.LogWarning("GameFlowManager: CountdownManager não foi atribuído. A partida começará imediatamente.");
            BeginPlaying();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void BeginPlaying()
    {
        if (state != GameState.Countdown)
            return;

        state = GameState.Playing;
    }

    public void KillPlayer()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.DeathSequence;

        StopActors();

        if (playerPathRecorder != null)
            playerPathRecorder.DiscardCurrentAttempt();

        if (aiMemoryManager != null)
            aiMemoryManager.DiscardPendingAttempt();

        PlayOneShot(deathClip);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (deathCanvasDelay > 0f)
            yield return new WaitForSecondsRealtime(deathCanvasDelay);

        if (deathCanvas != null)
            deathCanvas.SetActive(true);

        state = GameState.Dead;
    }

    public void PlayerWon()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.Victory;
        StopActors();

        if (playerPathRecorder != null && playerMovement != null)
        {
            // Garante que a célula em direção à saída entre no histórico mesmo
            // caso o Trigger seja atingido antes do centro exato da célula.
            playerPathRecorder.RecordCell(playerMovement.CurrentOrTargetCell);
        }

        if (aiMemoryManager != null && playerPathRecorder != null)
            aiMemoryManager.StageSuccessfulAttempt(playerPathRecorder.GetCurrentPathCopy());

        PlayOneShot(victoryClip);

        if (victoryCanvas != null)
            victoryCanvas.SetActive(true);
    }

    public void RestartAfterDeath()
    {
        if (sceneLoadRequested)
            return;

        if (aiMemoryManager != null)
            aiMemoryManager.DiscardPendingAttempt();

        LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void StartNextLevel()
    {
        if (state != GameState.Victory || sceneLoadRequested)
            return;

        if (aiMemoryManager != null)
            aiMemoryManager.CommitPendingWin();

        LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        if (sceneLoadRequested)
            return;

        // Voltar ao menu não confirma a rota da vitória.
        // A memória só é consolidada ao clicar em "Nova Fase".
        if (aiMemoryManager != null)
            aiMemoryManager.DiscardPendingAttempt();

        LoadSceneAsync(mainMenuSceneIndex);
    }

    private void StopActors()
    {
        if (playerMovement != null)
            playerMovement.StopMovement();

        if (enemyMovement != null)
            enemyMovement.StopMovement();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void LoadSceneAsync(int buildIndex)
    {
        if (sceneLoadRequested)
            return;

        sceneLoadRequested = true;
        state = GameState.Transition;
        SceneManager.LoadSceneAsync(buildIndex);
    }
}
