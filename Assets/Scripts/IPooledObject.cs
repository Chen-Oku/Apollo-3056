public interface IPooledObject
{
    // Llamado cuando el objeto es tomado del pool (activado)
    void OnObjectSpawn();

    // Llamado cuando el objeto es devuelto al pool (desactivado)
    void OnObjectReturn();
}
