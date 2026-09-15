using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private bool isLoading;

    public void StartGame(int sceneIndex)
    {
        if (isLoading)
            return;

        if (sceneIndex < 0 ||
            sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                $"MainMenuManager: Scene Index inválido: {sceneIndex}",
                this
            );

            return;
        }

        isLoading = true;

        SceneManager.LoadSceneAsync(sceneIndex);
    }

    public void DeleteAISave(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogWarning(
                "MainMenuManager: nome do arquivo de save não informado.",
                this
            );

            return;
        }

        string savePath = Path.Combine(
            Application.persistentDataPath,
            fileName
        );

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
                $"Nenhum save encontrado em:\n{savePath}"
            );
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}