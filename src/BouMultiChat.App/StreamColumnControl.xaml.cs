using System.IO;
using System.Windows;
using System.Windows.Controls;
using BouMultiChat.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BouMultiChat.App;

/// <summary>
/// Affiche un chat et un lecteur dans des vues web isolées et verrouillées.
/// </summary>
public sealed partial class StreamColumnControl : UserControl, IDisposable
{
    private readonly StreamColumnDefinition definition;
    private bool disposed;

    /// <summary>
    /// Initialise une colonne à partir d’une définition déjà validée.
    /// </summary>
    /// <param name="definition">Définition sûre de la colonne.</param>
    internal StreamColumnControl(StreamColumnDefinition definition)
    {
        this.definition = definition;
        InitializeComponent();

        TitleTextBlock.Text = definition.DisplayName;
        PlatformBadgeTextBlock.Text = definition.Platform.ToString().ToUpperInvariant();
        IdentifierTextBlock.Text = definition.Identifier;
        Loaded += StreamColumnControlLoaded;
    }

    /// <summary>Signale à la fenêtre principale que l’utilisateur souhaite retirer la colonne.</summary>
    internal event EventHandler? RemoveRequested;

    /// <summary>
    /// Libère immédiatement les processus WebView2 associés à la colonne.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Loaded -= StreamColumnControlLoaded;
        ChatWebView.Dispose();
        VideoWebView.Dispose();
    }

    /// <summary>
    /// Démarre une seule fois l’environnement WebView2 lorsque le contrôle devient visible.
    /// </summary>
    /// <param name="sender">Contrôle chargé.</param>
    /// <param name="e">Informations de chargement WPF.</param>
    private async void StreamColumnControlLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= StreamColumnControlLoaded;

        try
        {
            await InitializeWebViewsAsync();
            LoadingTextBlock.Visibility = Visibility.Collapsed;
        }
        catch (Exception exception) when (exception is WebView2RuntimeNotFoundException or InvalidOperationException)
        {
            ShowSafeError("Le moteur WebView2 n’a pas pu être initialisé. Vérifiez que son runtime est installé.");
        }
    }

    /// <summary>
    /// Crée un profil privé à la colonne et configure ses deux vues avant toute navigation.
    /// </summary>
    /// <returns>Tâche représentant l’initialisation complète.</returns>
    private async Task InitializeWebViewsAsync()
    {
        string profileFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BouMultiChat",
            "WebView2",
            Guid.NewGuid().ToString("N"));

        CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profileFolder);
        await ConfigureWebViewAsync(ChatWebView, definition.Addresses.Chat, environment);
        await ConfigureWebViewAsync(VideoWebView, definition.Addresses.Video, environment);
    }

    /// <summary>
    /// Verrouille une vue web, branche ses contrôles de navigation puis charge son adresse approuvée.
    /// </summary>
    /// <param name="webView">Vue à sécuriser.</param>
    /// <param name="address">Adresse déjà validée par le cœur métier.</param>
    /// <param name="environment">Environnement privé partagé dans la colonne.</param>
    /// <returns>Tâche terminée lorsque la vue peut naviguer.</returns>
    private async Task ConfigureWebViewAsync(
        WebView2 webView,
        Uri address,
        CoreWebView2Environment environment)
    {
        webView.AllowExternalDrop = false;
        await webView.EnsureCoreWebView2Async(environment);

        CoreWebView2Settings settings = webView.CoreWebView2.Settings;
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsStatusBarEnabled = false;

        webView.CoreWebView2.NavigationStarting += NavigationStarting;
        webView.CoreWebView2.NewWindowRequested += NewWindowRequested;
        webView.CoreWebView2.DownloadStarting += DownloadStarting;
        webView.CoreWebView2.PermissionRequested += PermissionRequested;
        webView.CoreWebView2.ProcessFailed += ProcessFailed;
        webView.Source = address;
    }

    /// <summary>
    /// Annule toute navigation qui sort du domaine officiel de la plateforme.
    /// </summary>
    /// <param name="sender">Moteur ayant demandé la navigation.</param>
    /// <param name="e">Adresse et état de la navigation.</param>
    private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!EmbedUrlValidator.TryValidate(e.Uri, definition.Platform, out _))
        {
            e.Cancel = true;
            ShowSafeError("Une navigation vers un domaine non autorisé a été bloquée.");
        }
    }

    /// <summary>
    /// Bloque les fenêtres secondaires afin d’empêcher une sortie de la vue contrôlée.
    /// </summary>
    /// <param name="sender">Moteur ayant demandé la fenêtre.</param>
    /// <param name="e">Demande de nouvelle fenêtre.</param>
    private static void NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
    }

    /// <summary>
    /// Refuse tout téléchargement initié par une page intégrée.
    /// </summary>
    /// <param name="sender">Moteur ayant initié le téléchargement.</param>
    /// <param name="e">Téléchargement demandé.</param>
    private static void DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        e.Cancel = true;
    }

    /// <summary>
    /// Refuse les accès caméra, microphone, presse-papiers et autres permissions sensibles.
    /// </summary>
    /// <param name="sender">Moteur ayant demandé la permission.</param>
    /// <param name="e">Permission demandée.</param>
    private static void PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        e.State = CoreWebView2PermissionState.Deny;
    }

    /// <summary>
    /// Signale une défaillance du processus web sans exposer de détail technique ou de contenu privé.
    /// </summary>
    /// <param name="sender">Moteur dont le processus a échoué.</param>
    /// <param name="e">Informations techniques volontairement non journalisées.</param>
    private void ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        ShowSafeError("Le processus du chat ou du lecteur s’est arrêté. Retirez puis recréez la colonne.");
    }

    /// <summary>
    /// Transmet la demande de suppression à la fenêtre propriétaire.
    /// </summary>
    /// <param name="sender">Bouton ayant déclenché l’action.</param>
    /// <param name="e">Informations du clic.</param>
    private void RemoveButtonClick(object sender, RoutedEventArgs e)
    {
        RemoveRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Affiche une erreur générique qui ne révèle ni URL complète ni contenu du chat.
    /// </summary>
    /// <param name="message">Message sûr destiné à l’utilisateur.</param>
    private void ShowSafeError(string message)
    {
        LoadingTextBlock.Visibility = Visibility.Collapsed;
        ErrorTextBlock.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}
