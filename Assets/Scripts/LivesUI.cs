using UnityEngine;
using TMPro;

public class LivesUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public TextMeshProUGUI livesText;

    void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged.AddListener(OnLivesChanged);
            OnLivesChanged(playerHealth.CurrentLives);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnLivesChanged.RemoveListener(OnLivesChanged);
    }

    void OnLivesChanged(int lives)
    {
        if (livesText != null)
            livesText.text = "Vidas: " + lives;
    }
}
