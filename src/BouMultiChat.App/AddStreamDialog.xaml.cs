using System.Windows;
using System.Windows.Controls;
using BouMultiChat.Core;

namespace BouMultiChat.App;

/// <summary>
/// Recueille et valide les informations nécessaires à une nouvelle colonne.
/// </summary>
public partial class AddStreamDialog : Window
{
    /// <summary>
    /// Initialise la boîte de dialogue d’ajout d’un stream.
    /// </summary>
    public AddStreamDialog()
    {
        InitializeComponent();
    }

    /// <summary>Obtient la définition validée après confirmation.</summary>
    internal StreamColumnDefinition? Definition { get; private set; }

    /// <summary>
    /// Valide les champs et ferme la boîte de dialogue lorsque la colonne est sûre.
    /// </summary>
    /// <param name="sender">Bouton ayant déclenché l’action.</param>
    /// <param name="e">Informations de l’événement de clic.</param>
    private void AddButtonClick(object sender, RoutedEventArgs e)
    {
        string displayName = DisplayNameTextBox.Text.Trim();
        string identifier = IdentifierTextBox.Text.Trim();

        if (displayName.Length is < 1 or > 40)
        {
            ShowError("Le nom de la colonne doit contenir entre 1 et 40 caractères.");
            return;
        }

        StreamingPlatform platform = ReadSelectedPlatform();
        if (!EmbedUrlFactory.TryCreate(platform, identifier, out EmbedAddresses? addresses, out string reason))
        {
            ShowError(reason);
            return;
        }

        Definition = new StreamColumnDefinition(displayName, platform, identifier, addresses!);
        DialogResult = true;
    }

    /// <summary>
    /// Ferme la boîte de dialogue sans créer de colonne.
    /// </summary>
    /// <param name="sender">Bouton ayant déclenché l’action.</param>
    /// <param name="e">Informations de l’événement de clic.</param>
    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    /// <summary>
    /// Convertit le choix visuel en valeur métier sans accepter de texte arbitraire.
    /// </summary>
    /// <returns>Plateforme correspondant à l’option prédéfinie.</returns>
    private StreamingPlatform ReadSelectedPlatform()
    {
        return PlatformComboBox.SelectedItem is ComboBoxItem { Tag: "YouTube" }
            ? StreamingPlatform.YouTube
            : StreamingPlatform.Twitch;
    }

    /// <summary>
    /// Affiche un motif de refus au plus près des champs concernés.
    /// </summary>
    /// <param name="message">Message français ne contenant aucune donnée sensible.</param>
    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
        IdentifierTextBox.Focus();
    }
}
