using UnityEngine;

// Coloca este GameObject fuera de la cámara con BoxCollider (isTrigger)
// y opcionalmente configura "destroyTags". Si el objeto tiene componente
// ObstacleDodge se devolverá al pool; en caso contrario se intentará destruir.
public class DestroyZone : MonoBehaviour
{
    public string[] destroyTags = new string[] { "Obstacle" };

    void OnTriggerExit(Collider other)
    {
        // Si el objeto tiene el componente ObstacleDodge, tratamos como obstáculo
        if (other.GetComponent<ObstacleDodge>() != null)
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.ReturnToPool(other.gameObject);
            else
                Destroy(other.gameObject);
            return;
        }

        // Si tiene tags que queremos filtrar
        foreach (var t in destroyTags)
        {
            if (other.CompareTag(t))
            {
                if (PoolManager.Instance != null)
                    PoolManager.Instance.ReturnToPool(other.gameObject);
                else
                    Destroy(other.gameObject);
                return;
            }
        }
    }
    
}
