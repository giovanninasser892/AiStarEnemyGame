using UnityEngine;

public class EnemyCatchDetector2D : MonoBehaviour
{
    [Header("Trigger do Player")]
    [SerializeField] private string playerDeathTriggerTag = "PlayerDeathTrigger";

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCatch(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCatch(other);
    }

    private void TryCatch(Collider2D other)
    {
        if (other == null)
            return;

        if (!other.CompareTag(playerDeathTriggerTag))
            return;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.KillPlayer();
    }
}
