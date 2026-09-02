namespace BouMultiChat.Core;

/// <summary>
/// Décrit l’état observé de BouVideoServ et de son moteur RTMP local.
/// </summary>
public sealed class RtmpServerStatus
{
    /// <summary>
    /// Initialise un instantané de l’état du serveur local.
    /// </summary>
    /// <param name="apiAvailable">Indique si l’API BouVideoServ répond.</param>
    /// <param name="rtmpAvailable">Indique si le port RTMP accepte une connexion.</param>
    /// <param name="engineRunning">Indique si BouVideoServ déclare MediaMTX actif.</param>
    /// <param name="canManage">Indique si BouMultiChat peut arrêter le processus identifié.</param>
    /// <param name="message">Résumé français destiné à l’interface.</param>
    public RtmpServerStatus(
        bool apiAvailable,
        bool rtmpAvailable,
        bool engineRunning,
        bool canManage,
        string message)
    {
        ApiAvailable = apiAvailable;
        RtmpAvailable = rtmpAvailable;
        EngineRunning = engineRunning;
        CanManage = canManage;
        Message = message;
    }

    /// <summary>Obtient une valeur indiquant si l’API locale répond.</summary>
    public bool ApiAvailable { get; }

    /// <summary>Obtient une valeur indiquant si le port RTMP répond.</summary>
    public bool RtmpAvailable { get; }

    /// <summary>Obtient une valeur indiquant si MediaMTX est déclaré actif.</summary>
    public bool EngineRunning { get; }

    /// <summary>Obtient une valeur indiquant si le processus peut être piloté sûrement.</summary>
    public bool CanManage { get; }

    /// <summary>Obtient le résumé français de l’état.</summary>
    public string Message { get; }

    /// <summary>Obtient une valeur indiquant si l’ensemble API et RTMP est opérationnel.</summary>
    public bool IsReady => ApiAvailable && RtmpAvailable && EngineRunning;
}
