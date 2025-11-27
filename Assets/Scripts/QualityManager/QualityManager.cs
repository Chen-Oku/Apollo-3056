using UnityEngine;

// Define los niveles de calidad disponibles. Los valores (0, 1, 2)
// deben coincidir con los índices de Calidad definidos en Unity.
public enum QualityLevelProfe
{
    Low = 0,    // Nivel 0 en Project Settings
    Medium = 1, // Nivel 1 en Project Settings
    High = 2    // Nivel 2 en Project Settings
}

public class QualityManager : MonoBehaviour
{
    void Start()
    {
        // 1. Detectar el nivel de calidad óptimo
        QualityLevel detectedLevel = DetectQualityLevel(); 

        // 2. Aplicar el nivel de calidad detectado
        // Convertimos el enum (0, 1 o 2) a int para usarlo como índice. 
        // El parámetro 'true' fuerza la aplicación inmediata del Render Pipeline Asset.
        QualitySettings.SetQualityLevel((int)detectedLevel);

        Debug.Log($"Calidad asignada automáticamente: {detectedLevel}");
    }

    /// <summary>
    /// Calcula una puntuación de hardware basada en múltiples métricas clave para móviles.
    /// </summary>
    public QualityLevel DetectQualityLevel()
    {
        int score = 0;

        // La puntuación máxima teórica es 10.

        // --- 1. RAM (System Memory Size en MB) ---
        // 1 GB = 1024 MB.
        if (SystemInfo.systemMemorySize >= 5000) score += 2; // >= 5 GB
        else if (SystemInfo.systemMemorySize >= 2500) score += 1; // >= 2.5 GB

        // --- 2. CPU CORES (Número de núcleos) ---
        if (SystemInfo.processorCount >= 8) score += 2; // 8 núcleos o más
        else if (SystemInfo.processorCount >= 6) score += 1; // 6 núcleos

        // --- 3. CPU FRECUENCIA (MHz) ---
        if (SystemInfo.processorFrequency >= 2200) score += 2; // >= 2.2 GHz
        else if (SystemInfo.processorFrequency >= 1500) score += 1; // >= 1.5 GHz

        // --- 4. GPU MEMORIA (VRAM en MB) ---
        if (SystemInfo.graphicsMemorySize >= 1200) score += 2; // >= 1.2 GB
        else if (SystemInfo.graphicsMemorySize >= 700) score += 1; // >= 700 MB

        // --- 5. SHADER LEVEL ---
        // Nivel de soporte de Shaders
        if (SystemInfo.graphicsShaderLevel >= 45) score += 2;
        else if (SystemInfo.graphicsShaderLevel >= 35) score += 1;

        // --- 6. PENALIZACIÓN POR GPU BAJA GAMA CONOCIDA ---
        string gpuModel = SystemInfo.graphicsDeviceName.ToLower();
        if (gpuModel.Contains("adreno 3") || gpuModel.Contains("mali-400")) score -= 2;

        // --- LÓGICA DE DECISIÓN ---
        // High: Score >= 8 (Dispositivos muy potentes)
        if (score >= 8) return QualityLevel.High;

        // Medium: Score >= 6 (Dispositivos de gama media/buen equilibrio)
        if (score >= 6) return QualityLevel.Medium;

        // Low: Score < 6 (Dispositivos básicos)
        return QualityLevel.Low;
    }
}