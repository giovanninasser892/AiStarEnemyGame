using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Cenas")]
    [Min(0)]
    [SerializeField] private int gameplaySceneIndex = 1;

    [Header("Save da IA")]
    [SerializeField] private string aiSaveFileName = "maze_ai_memory.json";

    private bool isLoading;

    public void StartGame()
    {
        if (isLoading)
            return;

        isLoading = true;

        SceneManager.LoadSceneAsync(gameplaySceneIndex);
    }

    public void DeleteAISave()
    {
        string savePath =
            Path.Combine(
                Application.persistentDataPath,
                aiSaveFileName
            );

        // Caso exista um AIMemoryManager ativo na cena.
        if (AIMemoryManager.Instance != null)
        {
            AIMemoryManager.Instance.ResetMemory();
            return;
        }

        // Caso estejamos somente no Main Menu e não exista
        // AIMemoryManager carregado.
        if (File.Exists(savePath))
        {
            File.Delete(savePath);

            Debug.Log(
                $"Save da IA excluído com sucesso:\n{savePath}"
            );
        }
        else
        {
            Debug.Log(
                $"Nenhum save da IA encontrado em:\n{savePath}"
            );
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log(
            "QuitGame chamado. " +
            "Application.Quit não fecha o Unity Editor."
        );
#else
        Application.Quit();
#endif
    }
}