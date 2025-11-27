using UnityEngine;

//Definir los Levels de Quality disponibles. Low, Medium, High (0, 1, 2)
//Deben coincidir con los Levels de Quality definidos en Unity

public enum QualityLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

public class QualityManagement : MonoBehaviour
{
    private void Start()
    {
        // 1. Detectar el nivel de calidad optimo para el dispositivo
        QualityLevel detectedLevel = DetectQualityLevel();

        // 2. Aplicar el nivel de calidad detectado
        // Convertimos el enum (0, 1 o 2) a int para usarlo como indice
        // El parametro 'true' fuerza la aplicacion inmediata del Render Pipeline Asset
        QualitySettings.SetQualityLevel((int)detectedLevel);

        Debug.Log($"Calidad asignada automáticamente: {detectedLevel}");
    }

    public QualityLevel DetectQualityLevel()
    {
        int score = 0;

        // --- 1. RAM (System Memory Size en MB) ---
        // 1 GB = 1024 MB
        if(SystemInfo.systemMemorySize >= 5000) score += 2; //>= 5 GB
        else if(SystemInfo.systemMemorySize >= 2500) score += 1; //>= 3 GB
        
        // --- 2. CPU CORES (Numero de nucleos) ---
        if(SystemInfo.processorCount >= 8) score += 2; //8 o mas nucleos
        else if(SystemInfo.processorCount >= 6) score += 1; //6 nucleos 

        // --- 3. CPU Frecuencia (Processor Frequency en MHz) ---
        if(SystemInfo.processorFrequency >= 2200) score += 2; //>= 2.2 GHz
        else if(SystemInfo.processorFrequency >= 1500) score += 1; //>= 1.5 GHz

        // --- 4. GPU Memoria (Graphics Memory Size en MB) ---
        if(SystemInfo.graphicsMemorySize >= 1200) score += 2; //>= 1.2 GB
        else if(SystemInfo.graphicsMemorySize >= 700) score += 1; //>= 700 MB

        // --- 5. SHADER LEVEL (Graphics Shader Level) ---
        if(SystemInfo.graphicsShaderLevel >= 45) score += 2; //Shader Model 4.5 o superior
        else if(SystemInfo.graphicsShaderLevel >= 35) score += 1; //Shader Model 3.5 o superior

        // --- 6. PENALIZACION POR GPU BAJA GAMA CONOCIDA ---
        string gpuName = SystemInfo.graphicsDeviceName.ToLower();

        if(gpuName.Contains("adreno 3") || gpuName.Contains("mali-400")) score -= 2; //GPU baja gama conocida

        // --- Logica de Decision ---
        // High: Score >= 8 (Dispositivos muy potentes)
        if(score >= 8) return QualityLevel.High;

        // Medium: 4 <= Score < 8 (Dispositivos de gama media)
        else if(score >= 5) return QualityLevel.Medium;

        // Low: Score < 5 (Dispositivos de gama baja)
        else return QualityLevel.Low;
    }
}
