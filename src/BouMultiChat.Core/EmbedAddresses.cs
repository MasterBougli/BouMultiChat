namespace BouMultiChat.Core;

/// <summary>
/// Regroupe les adresses validées du chat et du lecteur d’une diffusion.
/// </summary>
public sealed class EmbedAddresses
{
    /// <summary>
    /// Initialise un couple d’adresses déjà validées par la politique de navigation.
    /// </summary>
    /// <param name="chat">Adresse HTTPS du chat intégré.</param>
    /// <param name="video">Adresse HTTPS du lecteur vidéo intégré.</param>
    public EmbedAddresses(Uri chat, Uri video)
    {
        Chat = chat;
        Video = video;
    }

    /// <summary>Obtient l’adresse du chat intégré.</summary>
    public Uri Chat { get; }

    /// <summary>Obtient l’adresse du lecteur vidéo intégré.</summary>
    public Uri Video { get; }
}
