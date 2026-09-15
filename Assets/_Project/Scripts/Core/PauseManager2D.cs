using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager2D : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameObject pauseCanvas;

    [Header("Animações da Gameplay")]
    [Tooltip("Animators do Player, Enemy e outros objetos que devem congelar durante o pause.")]
    [SerializeField] private Animator[] gameplayAnimators;

    [Header("Áudio")]
    [SerializeField] private bool pauseGameplayAudio = true;

    private float[] animatorSpeeds;

    private void Awake()
    {
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        CacheAnimatorSpeeds();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (!keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (GameFlowManager.Instance == null)
            return;

        if (GameFlowManager.Instance.State == GameState.Playing)
        {
            OpenPause();
        }
        else if (GameFlowManager.Instance.State == GameState.Paused)
        {
            ContinueGame();
        }
    }

    public void OpenPause()
    {
        if (GameFlowManager.Instance == null)
            return;

        if (!GameFlowManager.Instance.PauseGame())
            return;

        PauseGameplayAnimators();

        if (pauseGameplayAudio)
            AudioListener.pause = true;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);
    }

    public void ContinueGame()
    {
        if (GameFlowManager.Instance == null)
            return;

        if (!GameFlowManager.Instance.ResumeGame())
            return;

        ResumeGameplayAnimators();

        if (pauseGameplayAudio)
            AudioListener.pause = false;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    public void RestartScene()
    {
        PrepareForSceneChange();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.RestartCurrentScene();
    }

    public void ReturnToMainMenu()
    {
        PrepareForSceneChange();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ReturnToMenu();
    }

    private void PrepareForSceneChange()
    {
        AudioListener.pause = false;

        ResumeGameplayAnimators();

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    private void CacheAnimatorSpeeds()
    {
        if (gameplayAnimators == null)
            return;

        animatorSpeeds = new float[gameplayAnimators.Length];

        for (int i = 0; i < gameplayAnimators.Length; i++)
        {
            if (gameplayAnimators[i] != null)
                animatorSpeeds[i] = gameplayAnimators[i].speed;
            else
                animatorSpeeds[i] = 1f;
        }
    }

    private void PauseGameplayAnimators()
    {
        if (gameplayAnimators == null)
            return;

        for (int i = 0; i < gameplayAnimators.Length; i++)
        {
            if (gameplayAnimators[i] != null)
                gameplayAnimators[i].speed = 0f;
        }
    }

    private void ResumeGameplayAnimators()
    {
        if (gameplayAnimators == null)
            return;

        for (int i = 0; i < gameplayAnimators.Length; i++)
        {
            if (gameplayAnimators[i] != null)
            {
                float speed = 1f;

                if (animatorSpeeds != null &&
                    i < animatorSpeeds.Length)
                {
                    speed = animatorSpeeds[i];
                }

                gameplayAnimators[i].speed = speed;
            }
        }
    }

    private void OnDestroy()
    {
        AudioListener.pause = false;
    }
}