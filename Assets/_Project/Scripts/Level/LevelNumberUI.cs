using TMPro;
using UnityEngine;

public class LevelNumberUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private TMP_Text levelText;

    [Header("Texto")]
    [SerializeField] private string prefix = "Fase ";

    private void Start()
    {
        UpdateLevelText();
    }

    public void UpdateLevelText()
    {
        if (levelText == null)
        {
            Debug.LogWarning("LevelNumberUI: TMP_Text não foi atribuído.", this);
            return;
        }

        int currentLevel = 1;

        if (AIMemoryManager.Instance != null)
            currentLevel = AIMemoryManager.Instance.CurrentLevel;

        levelText.text = prefix + currentLevel;
    }
}