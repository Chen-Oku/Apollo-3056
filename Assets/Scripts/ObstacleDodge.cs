using UnityEngine;

public class ObstacleDodge : MonoBehaviour
{

    public GameController gameController;
    public int scoreValue;

    void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Obstacle")) {
            // Sumar puntuación y devolver este obstáculo al pool (no destruir la zona)
            if (gameController != null)
                gameController.AddScore(scoreValue);

            if (PoolManager.Instance != null)
                PoolManager.Instance.ReturnToPool(other.gameObject);
            else
                Destroy(other.gameObject);
        }
    }

}
