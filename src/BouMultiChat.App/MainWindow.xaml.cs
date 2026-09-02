using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BouMultiChat.Core;
using Microsoft.Win32;

namespace BouMultiChat.App;

/// <summary>
/// Affiche l’espace principal de supervision des chats et des retours vidéo.
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    private const int MaximumColumnCount = 4;
    private readonly ColumnSettingsStore settingsStore = ColumnSettingsStore.CreateDefault();
    private readonly RtmpServerController rtmpController = new();
    private readonly DispatcherTimer rtmpStatusTimer;
    private readonly CancellationTokenSource windowLifetime = new();
    private bool rtmpCommandRunning;
    private bool disposed;

    /// <summary>
    /// Initialise la fenêtre principale et ses composants visuels.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        rtmpStatusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        rtmpStatusTimer.Tick += RtmpStatusTimerTick;
    }

    /// <summary>
    /// Libère le minuteur, le contrôleur RTMP et toutes les vues web sans arrêter BouVideoServ.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        windowLifetime.Cancel();
        rtmpStatusTimer.Stop();
        rtmpStatusTimer.Tick -= RtmpStatusTimerTick;
        rtmpController.Dispose();
        windowLifetime.Dispose();

        foreach (StreamColumnControl column in ColumnsGrid.Children.OfType<StreamColumnControl>())
        {
            column.RemoveRequested -= RemoveColumnRequested;
            column.Dispose();
        }

        ColumnsGrid.Children.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Recharge automatiquement les colonnes locales après l’initialisation de la fenêtre.
    /// </summary>
    /// <param name="sender">Fenêtre devenue disponible.</param>
    /// <param name="e">Informations de chargement.</param>
    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= WindowLoaded;
        int restoredCount = 0;

        foreach (SavedStreamColumn saved in settingsStore.Load())
        {
            if (restoredCount >= MaximumColumnCount)
            {
                break;
            }

            if (!StreamColumnDefinition.TryRestore(saved, out StreamColumnDefinition? definition))
            {
                continue;
            }

            AddColumn(definition!);
            restoredCount++;
        }

        UpdateColumnLayout();
        SaveStatusTextBlock.Text = restoredCount == 0
            ? "Sauvegarde locale"
            : $"{restoredCount} colonne(s) restaurée(s)";

        await RefreshRtmpStatusAsync();
        rtmpStatusTimer.Start();
    }

    /// <summary>
    /// Actualise périodiquement l’état du serveur sans lancer de commande.
    /// </summary>
    /// <param name="sender">Minuteur de vérification.</param>
    /// <param name="e">Informations du déclenchement.</param>
    private async void RtmpStatusTimerTick(object? sender, EventArgs e)
    {
        if (!rtmpCommandRunning)
        {
            await RefreshRtmpStatusAsync();
        }
    }

    /// <summary>
    /// Ouvre le sélecteur Windows puis mémorise uniquement un binaire BouVideoServ valide.
    /// </summary>
    /// <param name="sender">Bouton de sélection.</param>
    /// <param name="e">Informations du clic.</param>
    private async void ChooseRtmpServerButtonClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "Sélectionner BouVideoServ",
            Filter = "BouVideoServ (bouvideoserv.exe)|bouvideoserv.exe",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (!rtmpController.TrySetExecutablePath(dialog.FileName, out string reason))
        {
            MessageBox.Show(this, reason, "Serveur RTMP refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RefreshRtmpStatusAsync();
    }

    /// <summary>
    /// Demande le démarrage du serveur local sélectionné.
    /// </summary>
    /// <param name="sender">Bouton de démarrage.</param>
    /// <param name="e">Informations du clic.</param>
    private async void StartRtmpServerButtonClick(object sender, RoutedEventArgs e)
    {
        await RunRtmpCommandAsync(
            cancellationToken => rtmpController.StartAsync(cancellationToken),
            "Démarrage de BouVideoServ…");
    }

    /// <summary>
    /// Demande le redémarrage sûr du serveur local sélectionné.
    /// </summary>
    /// <param name="sender">Bouton de redémarrage.</param>
    /// <param name="e">Informations du clic.</param>
    private async void RestartRtmpServerButtonClick(object sender, RoutedEventArgs e)
    {
        await RunRtmpCommandAsync(
            cancellationToken => rtmpController.RestartAsync(cancellationToken),
            "Redémarrage de BouVideoServ…");
    }

    /// <summary>
    /// Demande l’arrêt du seul processus correspondant au binaire approuvé.
    /// </summary>
    /// <param name="sender">Bouton d’arrêt.</param>
    /// <param name="e">Informations du clic.</param>
    private async void StopRtmpServerButtonClick(object sender, RoutedEventArgs e)
    {
        await RunRtmpCommandAsync(
            cancellationToken => rtmpController.StopAsync(cancellationToken),
            "Arrêt de BouVideoServ…");
    }

    /// <summary>
    /// Sérialise une commande RTMP, actualise l’interface et masque les détails techniques des erreurs.
    /// </summary>
    /// <param name="command">Commande locale à exécuter.</param>
    /// <param name="pendingMessage">Texte affiché pendant l’opération.</param>
    private async Task RunRtmpCommandAsync(
        Func<CancellationToken, Task<RtmpServerStatus>> command,
        string pendingMessage)
    {
        if (rtmpCommandRunning)
        {
            return;
        }

        rtmpCommandRunning = true;
        SetRtmpButtonsEnabled(false);
        RtmpStatusTextBlock.Text = pendingMessage;

        try
        {
            RtmpServerStatus status = await command(windowLifetime.Token);
            UpdateRtmpStatus(status);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
                or IOException
                or UnauthorizedAccessException
                or System.ComponentModel.Win32Exception)
        {
            RtmpStatusTextBlock.Text = exception.Message;
            RtmpStatusEllipse.Fill = (Brush)FindResource("DangerBrush");
        }
        catch (Exception exception) when (
            disposed && exception is OperationCanceledException or ObjectDisposedException)
        {
            // La fermeture de la fenêtre annule proprement la commande en cours.
        }
        finally
        {
            rtmpCommandRunning = false;
            if (!disposed)
            {
                await RefreshRtmpStatusAsync();
            }
        }
    }

    /// <summary>
    /// Interroge BouVideoServ et actualise les indicateurs sans propager une panne réseau.
    /// </summary>
    private async Task RefreshRtmpStatusAsync()
    {
        try
        {
            RtmpServerStatus status = await rtmpController.GetStatusAsync(windowLifetime.Token);
            UpdateRtmpStatus(status);
        }
        catch (Exception exception) when (exception is ObjectDisposedException or OperationCanceledException)
        {
            // La fenêtre se ferme pendant une vérification en cours.
        }
    }

    /// <summary>
    /// Traduit l’état technique du relais en informations visuelles et commandes disponibles.
    /// </summary>
    /// <param name="status">État local déjà borné.</param>
    private void UpdateRtmpStatus(RtmpServerStatus status)
    {
        RtmpStatusTextBlock.Text = status.Message;
        RtmpLinkTextBlock.Text =
            $"API {(status.ApiAvailable ? "✓" : "—")} · "
            + $"RTMP {(status.RtmpAvailable ? "✓" : "—")} · "
            + $"contrôle {(status.CanManage ? "✓" : "—")}";
        RtmpStatusEllipse.Fill = status.IsReady
            ? (Brush)FindResource("AccentBrush")
            : status.ApiAvailable || status.RtmpAvailable
                ? Brushes.Gold
                : (Brush)FindResource("SubtleTextBrush");

        RtmpChooseButton.ToolTip = rtmpController.ExecutablePath ?? "Aucun binaire sélectionné";
        RtmpStartButton.IsEnabled = !rtmpCommandRunning
            && rtmpController.HasExecutable
            && !status.ApiAvailable
            && !status.RtmpAvailable;
        RtmpRestartButton.IsEnabled = !rtmpCommandRunning && status.CanManage;
        RtmpStopButton.IsEnabled = !rtmpCommandRunning && status.CanManage;
        RtmpChooseButton.IsEnabled = !rtmpCommandRunning;
    }

    /// <summary>
    /// Active ou désactive simultanément les commandes du serveur RTMP.
    /// </summary>
    /// <param name="enabled">État appliqué aux boutons.</param>
    private void SetRtmpButtonsEnabled(bool enabled)
    {
        RtmpChooseButton.IsEnabled = enabled;
        RtmpStartButton.IsEnabled = enabled;
        RtmpRestartButton.IsEnabled = enabled;
        RtmpStopButton.IsEnabled = enabled;
    }

    /// <summary>
    /// Ouvre le formulaire local puis ajoute une colonne uniquement après validation complète.
    /// </summary>
    /// <param name="sender">Bouton ayant demandé l’ajout.</param>
    /// <param name="e">Informations du clic.</param>
    private void AddColumnButtonClick(object sender, RoutedEventArgs e)
    {
        if (ColumnsGrid.Children.Count >= MaximumColumnCount)
        {
            MessageBox.Show(
                this,
                "Cette première version accepte quatre colonnes simultanées.",
                "Limite atteinte",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        AddStreamDialog dialog = new() { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Definition is null)
        {
            return;
        }

        AddColumn(dialog.Definition);
        UpdateColumnLayout();
        SaveColumns();
    }

    /// <summary>
    /// Ajoute une définition validée à la grille et branche ses événements.
    /// </summary>
    /// <param name="definition">Définition sûre à afficher.</param>
    private void AddColumn(StreamColumnDefinition definition)
    {
        StreamColumnControl column = new(definition);
        column.RemoveRequested += RemoveColumnRequested;
        ColumnsGrid.Children.Add(column);
    }

    /// <summary>
    /// Retire la colonne demandée et libère immédiatement ses processus web.
    /// </summary>
    /// <param name="sender">Colonne ayant demandé son retrait.</param>
    /// <param name="e">Informations de l’événement.</param>
    private void RemoveColumnRequested(object? sender, EventArgs e)
    {
        if (sender is not StreamColumnControl column)
        {
            return;
        }

        column.RemoveRequested -= RemoveColumnRequested;
        ColumnsGrid.Children.Remove(column);
        column.Dispose();
        UpdateColumnLayout();
        SaveColumns();
    }

    /// <summary>
    /// Enregistre la disposition courante sans interrompre l’application en cas d’erreur disque.
    /// </summary>
    private void SaveColumns()
    {
        try
        {
            settingsStore.Save(
                ColumnsGrid.Children
                    .OfType<StreamColumnControl>()
                    .Select(column => column.Definition.ToSavedColumn()));
            SaveStatusTextBlock.Text = "Enregistré automatiquement";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SaveStatusTextBlock.Text = "Sauvegarde impossible";
        }
    }

    /// <summary>
    /// Adapte la grille au nombre de colonnes et affiche l’état vide lorsque nécessaire.
    /// </summary>
    private void UpdateColumnLayout()
    {
        int columnCount = ColumnsGrid.Children.Count;
        ColumnsGrid.Columns = Math.Max(1, columnCount);
        EmptyState.Visibility = columnCount == 0 ? Visibility.Visible : Visibility.Collapsed;
        ColumnCountTextBlock.Text = $"{columnCount} / {MaximumColumnCount} colonnes";
    }

    /// <summary>
    /// Libère toutes les vues web lorsque l’utilisateur ferme l’application.
    /// </summary>
    /// <param name="sender">Fenêtre en cours de fermeture.</param>
    /// <param name="e">Informations de fermeture.</param>
    private void WindowClosed(object? sender, EventArgs e)
    {
        Dispose();
    }
}
