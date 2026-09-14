using UnityEngine;

public class ExitGoal2D : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryWin(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryWin(other);
    }

    private void TryWin(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag))
            return;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.PlayerWon();
    }
}
