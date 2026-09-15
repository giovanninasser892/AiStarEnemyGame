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

    public bool CanGameplayMove =>
        state == GameState.Playing;

    public bool IsPaused =>
        state == GameState.Paused;

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
            Debug.LogWarning(
                "GameFlowManager: CountdownManager não foi atribuído. " +
                "A partida começará imediatamente.",
                this
            );

            BeginPlaying();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================
    // GAMEPLAY
    // =========================================================

    public void BeginPlaying()
    {
        if (state != GameState.Countdown)
            return;

        state = GameState.Playing;
    }

    // =========================================================
    // PAUSE
    // =========================================================

    public bool PauseGame()
    {
        if (state != GameState.Playing)
            return false;

        state = GameState.Paused;

        return true;
    }

    public bool ResumeGame()
    {
        if (state != GameState.Paused)
            return false;

        state = GameState.Playing;

        return true;
    }

    // =========================================================
    // MORTE
    // =========================================================

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
        {
            yield return new WaitForSecondsRealtime(
                deathCanvasDelay
            );
        }

        if (deathCanvas != null)
            deathCanvas.SetActive(true);

        state = GameState.Dead;
    }

    // =========================================================
    // VITÓRIA
    // =========================================================

    public void PlayerWon()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.Victory;

        StopActors();

        if (playerPathRecorder != null &&
            playerMovement != null)
        {
            /*
             * Garante que a última célula seja registrada.
             *
             * Isso ajuda caso o Trigger da saída seja atingido
             * antes de o Player chegar exatamente no centro
             * da célula.
             */
            playerPathRecorder.RecordCell(
                playerMovement.CurrentOrTargetCell
            );
        }

        if (aiMemoryManager != null &&
            playerPathRecorder != null)
        {
            aiMemoryManager.StageSuccessfulAttempt(
                playerPathRecorder.GetCurrentPathCopy()
            );
        }

        PlayOneShot(victoryClip);

        if (victoryCanvas != null)
            victoryCanvas.SetActive(true);
    }

    // =========================================================
    // PRÓXIMA FASE
    // =========================================================

    public void StartNextLevel()
    {
        if (state != GameState.Victory)
            return;

        if (sceneLoadRequested)
            return;

        /*
         * Somente aqui a vitória é realmente salva.
         *
         * - aprende a rota
         * - aumenta SuccessfulRuns
         * - aumenta CurrentLevel
         * - salva JSON
         */
        if (aiMemoryManager != null)
            aiMemoryManager.CommitPendingWin();

        RestartCurrentScene();
    }

    // =========================================================
    // REINICIAR CENA
    // =========================================================

    public void RestartCurrentScene()
    {
        if (sceneLoadRequested)
            return;

        /*
         * Reiniciar pelo Pause ou após morrer
         * NÃO deve ensinar nada para a IA.
         */
        if (aiMemoryManager != null)
            aiMemoryManager.DiscardPendingAttempt();

        int currentSceneIndex =
            SceneManager.GetActiveScene().buildIndex;

        LoadSceneAsync(currentSceneIndex);
    }

    /*
     * Mantido para não quebrar botões antigos
     * da tela de derrota.
     */
    public void RestartAfterDeath()
    {
        RestartCurrentScene();
    }

    // =========================================================
    // MAIN MENU
    // =========================================================

    public void ReturnToMenu()
    {
        if (sceneLoadRequested)
            return;

        /*
         * Se o jogador vencer e voltar para o Menu
         * sem clicar em "Nova Fase",
         * essa rota não é consolidada.
         */
        if (aiMemoryManager != null)
            aiMemoryManager.DiscardPendingAttempt();

        LoadSceneAsync(mainMenuSceneIndex);
    }

    // =========================================================
    // MOVIMENTO
    // =========================================================

    private void StopActors()
    {
        if (playerMovement != null)
            playerMovement.StopMovement();

        if (enemyMovement != null)
            enemyMovement.StopMovement();
    }

    // =========================================================
    // ÁUDIO
    // =========================================================

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null)
            return;

        if (clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    // =========================================================
    // CARREGAMENTO
    // =========================================================

    private void LoadSceneAsync(int buildIndex)
    {
        if (sceneLoadRequested)
            return;

        sceneLoadRequested = true;

        state = GameState.Transition;

        SceneManager.LoadSceneAsync(buildIndex);
    }
}