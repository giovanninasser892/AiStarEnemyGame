using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text countdownText;

    [Header("Tempo")]
    [Min(1)]
    [SerializeField] private int startNumber = 3;

    [Min(0.05f)]
    [SerializeField] private float numberDuration = 1f;

    [Min(0f)]
    [SerializeField] private float goDuration = 0.5f;

    [Header("Texto")]
    [SerializeField] private string goText = "GO!";

    [Header("Áudio opcional")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickClip;
    [SerializeField] private AudioClip goClip;

    private Coroutine countdownRoutine;

    public void StartCountdown(GameFlowManager gameFlow)
    {
        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        countdownRoutine = StartCoroutine(CountdownRoutine(gameFlow));
    }

    private IEnumerator CountdownRoutine(GameFlowManager gameFlow)
    {
        if (root != null)
            root.SetActive(true);

        for (int i = startNumber; i >= 1; i--)
        {
            if (countdownText != null)
                countdownText.text = i.ToString();

            PlayOneShot(tickClip);
            yield return new WaitForSecondsRealtime(numberDuration);
        }

        if (countdownText != null)
            countdownText.text = goText;

        PlayOneShot(goClip);

        if (goDuration > 0f)
            yield return new WaitForSecondsRealtime(goDuration);

        if (root != null)
            root.SetActive(false);

        countdownRoutine = null;

        if (gameFlow != null)
            gameFlow.BeginPlaying();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
