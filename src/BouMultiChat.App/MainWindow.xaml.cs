using System.IO;
using System.Windows;
using System.Windows.Controls;
using BouMultiChat.Core;

namespace BouMultiChat.App;

/// <summary>
/// Affiche l’espace principal de supervision des chats et des retours vidéo.
/// </summary>
public partial class MainWindow : Window
{
    private const int MaximumColumnCount = 4;
    private readonly ColumnSettingsStore settingsStore = ColumnSettingsStore.CreateDefault();

    /// <summary>
    /// Initialise la fenêtre principale et ses composants visuels.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Recharge automatiquement les colonnes locales après l’initialisation de la fenêtre.
    /// </summary>
    /// <param name="sender">Fenêtre devenue disponible.</param>
    /// <param name="e">Informations de chargement.</param>
    private void WindowLoaded(object sender, RoutedEventArgs e)
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
        foreach (StreamColumnControl column in ColumnsGrid.Children.OfType<StreamColumnControl>())
        {
            column.RemoveRequested -= RemoveColumnRequested;
            column.Dispose();
        }

        ColumnsGrid.Children.Clear();
    }
}
