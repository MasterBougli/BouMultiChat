using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace BouMultiChat.Core;

/// <summary>
/// Détecte et pilote uniquement une instance locale explicitement identifiée de BouVideoServ.
/// </summary>
public sealed class RtmpServerController : IDisposable
{
    private const string ExpectedExecutableName = "bouvideoserv.exe";
    private const int RtmpPort = 1935;
    private static readonly Uri ApiBaseAddress = new("http://127.0.0.1:8080");

    private readonly HttpClient httpClient;
    private readonly SemaphoreSlim commandLock = new(1, 1);
    private readonly string settingsPath;
    private Process? ownedProcess;
    private bool disposed;

    /// <summary>
    /// Initialise le contrôleur et recharge le dernier binaire approuvé.
    /// </summary>
    public RtmpServerController()
    {
        httpClient = new HttpClient
        {
            BaseAddress = ApiBaseAddress,
            Timeout = TimeSpan.FromSeconds(1)
        };

        string settingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BouMultiChat");
        settingsPath = Path.Combine(settingsFolder, "rtmp-server-path.txt");
        ExecutablePath = LoadExecutablePath() ?? FindKnownExecutable();
    }

    /// <summary>Obtient le chemin absolu du binaire approuvé, s’il existe.</summary>
    public string? ExecutablePath { get; private set; }

    /// <summary>Obtient une valeur indiquant si un binaire valide est disponible.</summary>
    public bool HasExecutable => ExecutablePath is not null;

    /// <summary>
    /// Valide puis mémorise le binaire choisi explicitement par l’utilisateur.
    /// </summary>
    /// <param name="path">Chemin du binaire BouVideoServ.</param>
    /// <param name="failureReason">Motif français du refus.</param>
    /// <returns><see langword="true"/> lorsque le chemin est accepté et sauvegardé.</returns>
    public bool TrySetExecutablePath(string? path, out string failureReason)
    {
        if (!TryNormalizeExecutablePath(path, out string? normalizedPath, out failureReason))
        {
            return false;
        }

        try
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrEmpty(directory))
            {
                failureReason = "Le dossier de configuration locale est introuvable.";
                return false;
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(settingsPath, normalizedPath!);
            ExecutablePath = normalizedPath;
            failureReason = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            failureReason = "Le chemin du serveur n’a pas pu être sauvegardé.";
            return false;
        }
    }

    /// <summary>
    /// Vérifie séparément l’API, le moteur déclaré et le port RTMP local.
    /// </summary>
    /// <param name="cancellationToken">Jeton permettant d’annuler la vérification.</param>
    /// <returns>Instantané borné de l’état du serveur.</returns>
    public async Task<RtmpServerStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        bool apiAvailable = await IsApiAvailableAsync(cancellationToken);
        bool engineRunning = apiAvailable && await IsEngineRunningAsync(cancellationToken);
        bool rtmpAvailable = await IsRtmpPortAvailableAsync(cancellationToken);
        using Process? manageableProcess = FindManageableProcess();
        bool canManage = manageableProcess is not null;

        string message = (apiAvailable, engineRunning, rtmpAvailable) switch
        {
            (true, true, true) => "Serveur RTMP opérationnel",
            (true, false, _) => "BouVideoServ répond, mais MediaMTX est arrêté",
            (true, true, false) => "MediaMTX démarre, le port RTMP ne répond pas encore",
            (false, _, true) => "Port RTMP actif sans liaison avec BouVideoServ",
            _ when !HasExecutable => "Serveur introuvable : sélectionnez bouvideoserv.exe",
            _ => "Serveur RTMP arrêté"
        };

        return new RtmpServerStatus(apiAvailable, rtmpAvailable, engineRunning, canManage, message);
    }

    /// <summary>
    /// Démarre BouVideoServ avec son dossier de travail et son binaire MediaMTX connus.
    /// </summary>
    /// <param name="cancellationToken">Jeton permettant d’annuler l’attente de démarrage.</param>
    /// <returns>État observé après la tentative.</returns>
    public async Task<RtmpServerStatus> StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await commandLock.WaitAsync(cancellationToken);
        try
        {
            return await StartCoreAsync(cancellationToken);
        }
        finally
        {
            commandLock.Release();
        }
    }

    /// <summary>
    /// Arrête uniquement le processus correspondant exactement au binaire approuvé.
    /// </summary>
    /// <param name="cancellationToken">Jeton permettant d’annuler l’attente d’arrêt.</param>
    /// <returns>État observé après la tentative.</returns>
    public async Task<RtmpServerStatus> StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await commandLock.WaitAsync(cancellationToken);
        try
        {
            return await StopCoreAsync(cancellationToken);
        }
        finally
        {
            commandLock.Release();
        }
    }

    /// <summary>
    /// Arrête puis redémarre l’instance locale approuvée.
    /// </summary>
    /// <param name="cancellationToken">Jeton permettant d’annuler l’opération.</param>
    /// <returns>État observé après le redémarrage.</returns>
    public async Task<RtmpServerStatus> RestartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await commandLock.WaitAsync(cancellationToken);
        try
        {
            await StopCoreAsync(cancellationToken);
            return await StartCoreAsync(cancellationToken);
        }
        finally
        {
            commandLock.Release();
        }
    }

    /// <summary>
    /// Libère les ressources de contrôle sans arrêter le serveur local.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ownedProcess?.Dispose();
        httpClient.Dispose();
        commandLock.Dispose();
    }

    /// <summary>
    /// Démarre le processus sans reprendre le verrou de commande déjà acquis.
    /// </summary>
    /// <param name="cancellationToken">Jeton d’annulation.</param>
    /// <returns>État obtenu après l’attente bornée.</returns>
    private async Task<RtmpServerStatus> StartCoreAsync(CancellationToken cancellationToken)
    {
        RtmpServerStatus currentStatus = await GetStatusAsync(cancellationToken);
        if (currentStatus.ApiAvailable)
        {
            return currentStatus;
        }

        if (ExecutablePath is null)
        {
            throw new InvalidOperationException("Sélectionnez d’abord le binaire bouvideoserv.exe.");
        }

        string workingDirectory = ResolveWorkingDirectory(ExecutablePath);
        ProcessStartInfo startInfo = new()
        {
            FileName = ExecutablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        string mediaTmxPath = Path.Combine(workingDirectory, "bin", "mediamtx.exe");
        if (File.Exists(mediaTmxPath))
        {
            startInfo.Environment["MEDIAMTX_BIN"] = mediaTmxPath;
        }

        ownedProcess?.Dispose();
        ownedProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("BouVideoServ n’a pas pu être démarré.");

        return await WaitForStateAsync(expectRunning: true, cancellationToken);
    }

    /// <summary>
    /// Arrête le processus sans reprendre le verrou de commande déjà acquis.
    /// </summary>
    /// <param name="cancellationToken">Jeton d’annulation.</param>
    /// <returns>État obtenu après l’attente bornée.</returns>
    private async Task<RtmpServerStatus> StopCoreAsync(CancellationToken cancellationToken)
    {
        Process? process = FindManageableProcess();
        if (process is null)
        {
            RtmpServerStatus status = await GetStatusAsync(cancellationToken);
            if (!status.ApiAvailable && !status.RtmpAvailable)
            {
                return status;
            }

            throw new InvalidOperationException(
                "Le serveur actif n’est pas celui qui a été sélectionné. Arrêt refusé par sécurité.");
        }

        using (process)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(cancellationToken);
        }

        ownedProcess?.Dispose();
        ownedProcess = null;
        return await WaitForStateAsync(expectRunning: false, cancellationToken);
    }

    /// <summary>
    /// Attend brièvement que l’API et le port RTMP atteignent l’état demandé.
    /// </summary>
    /// <param name="expectRunning">État final attendu.</param>
    /// <param name="cancellationToken">Jeton d’annulation.</param>
    /// <returns>Dernier état observé.</returns>
    private async Task<RtmpServerStatus> WaitForStateAsync(
        bool expectRunning,
        CancellationToken cancellationToken)
    {
        RtmpServerStatus status = await GetStatusAsync(cancellationToken);
        for (int attempt = 0; attempt < 12; attempt++)
        {
            bool reached = expectRunning ? status.IsReady : !status.ApiAvailable && !status.RtmpAvailable;
            bool engineUnavailable = expectRunning && status.ApiAvailable && !status.EngineRunning;
            if (reached || engineUnavailable)
            {
                return status;
            }

            await Task.Delay(500, cancellationToken);
            status = await GetStatusAsync(cancellationToken);
        }

        return status;
    }

    /// <summary>
    /// Vérifie que l’API de santé locale répond correctement.
    /// </summary>
    /// <param name="cancellationToken">Jeton d’annulation.</param>
    /// <returns><see langword="true"/> lorsque la réponse HTTP est positive.</returns>
    private async Task<bool> IsApiAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync("/api/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Lit l’état MediaMTX déclaré par BouVideoServ sans faire confiance aux autres champs JSON.
    /// </summary>
    /// <param name="cancellationToken">Jeton d’annulation.</param>
    /// <returns><see langword="true"/> lorsque le moteur se déclare actif.</returns>
    private async Task<bool> IsEngineRunningAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync("/api/engine", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return document.RootElement.TryGetProperty("running", out JsonElement running)
                && running.ValueKind is JsonValueKind.True;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Teste le port RTMP local avec une durée maximale indépendante de l’API HTTP.
    /// </summary>
    /// <param name="cancellationToken">Jeton d’annulation.</param>
    /// <returns><see langword="true"/> lorsqu’une connexion TCP locale est possible.</returns>
    private static async Task<bool> IsRtmpPortAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(1));
            using TcpClient client = new();
            await client.ConnectAsync(IPAddress.Loopback, RtmpPort, timeout.Token);
            return true;
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Retrouve un processus seulement lorsque son chemin correspond exactement au binaire approuvé.
    /// </summary>
    /// <returns>Processus pilotable, ou <see langword="null"/> en cas d’ambiguïté.</returns>
    private Process? FindManageableProcess()
    {
        if (ExecutablePath is null)
        {
            return null;
        }

        foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExpectedExecutableName)))
        {
            try
            {
                string? processPath = process.MainModule?.FileName;
                if (processPath is not null
                    && string.Equals(
                        Path.GetFullPath(processPath),
                        ExecutablePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return process;
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Un processus inaccessible n’est jamais considéré comme pilotable.
            }

            process.Dispose();
        }

        return null;
    }

    /// <summary>
    /// Recharge le chemin sauvegardé seulement s’il reste conforme et présent.
    /// </summary>
    /// <returns>Chemin sûr, ou <see langword="null"/>.</returns>
    private string? LoadExecutablePath()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return null;
            }

            string storedPath = File.ReadAllText(settingsPath);
            return TryNormalizeExecutablePath(storedPath, out string? normalizedPath, out _)
                ? normalizedPath
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Recherche les emplacements standards du sous-module et d’une publication Windows.
    /// </summary>
    /// <returns>Premier binaire sûr trouvé, ou <see langword="null"/>.</returns>
    private static string? FindKnownExecutable()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, ExpectedExecutableName),
            Path.Combine(AppContext.BaseDirectory, "BouVideoServ", "bin", ExpectedExecutableName),
            Path.Combine(Environment.CurrentDirectory, "external", "BouVideoServ", "bin", ExpectedExecutableName),
            Path.Combine(Environment.CurrentDirectory, "external", "BouVideoServ", "target", "release", ExpectedExecutableName)
        ];

        foreach (string candidate in candidates)
        {
            if (TryNormalizeExecutablePath(candidate, out string? normalizedPath, out _))
            {
                return normalizedPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Résout le dossier racine attendu par les chemins relatifs de BouVideoServ.
    /// </summary>
    /// <param name="executablePath">Chemin déjà validé du binaire.</param>
    /// <returns>Dossier de travail contenant les ressources web et média.</returns>
    private static string ResolveWorkingDirectory(string executablePath)
    {
        string binaryDirectory = Path.GetDirectoryName(executablePath)!;
        DirectoryInfo directory = new(binaryDirectory);
        if (string.Equals(directory.Name, "bin", StringComparison.OrdinalIgnoreCase)
            && directory.Parent is not null)
        {
            return directory.Parent.FullName;
        }

        if (string.Equals(directory.Name, "release", StringComparison.OrdinalIgnoreCase)
            && directory.Parent?.Parent?.Parent is not null)
        {
            return directory.Parent.Parent.FullName;
        }

        return binaryDirectory;
    }

    /// <summary>
    /// Normalise un chemin et refuse tout fichier autre que bouvideoserv.exe.
    /// </summary>
    /// <param name="path">Chemin potentiellement non fiable.</param>
    /// <param name="normalizedPath">Chemin absolu accepté.</param>
    /// <param name="failureReason">Motif français du refus.</param>
    /// <returns><see langword="true"/> lorsque le fichier correspond au serveur attendu.</returns>
    public static bool TryNormalizeExecutablePath(
        string? path,
        out string? normalizedPath,
        out string failureReason)
    {
        normalizedPath = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            failureReason = "Aucun binaire n’a été sélectionné.";
            return false;
        }

        try
        {
            string fullPath = Path.GetFullPath(path.Trim());
            if (!File.Exists(fullPath)
                || !string.Equals(Path.GetFileName(fullPath), ExpectedExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                failureReason = "Sélectionnez un fichier nommé bouvideoserv.exe.";
                return false;
            }

            if (File.GetAttributes(fullPath).HasFlag(FileAttributes.ReparsePoint))
            {
                failureReason = "Les liens symboliques vers le serveur sont refusés.";
                return false;
            }

            normalizedPath = fullPath;
            failureReason = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            failureReason = "Le chemin du serveur est invalide ou inaccessible.";
            return false;
        }
    }

    /// <summary>
    /// Empêche l’utilisation du contrôleur après sa libération.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
