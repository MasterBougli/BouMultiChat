using BouMultiChat.Core;

namespace BouMultiChat.App;

/// <summary>
/// Décrit une colonne approuvée avant sa création dans l’interface.
/// </summary>
internal sealed class StreamColumnDefinition
{
    /// <summary>
    /// Initialise une colonne à partir de données déjà validées.
    /// </summary>
    /// <param name="displayName">Nom visible choisi par l’utilisateur.</param>
    /// <param name="platform">Plateforme de diffusion.</param>
    /// <param name="identifier">Identifiant public normalisé.</param>
    /// <param name="addresses">Adresses officielles validées.</param>
    internal StreamColumnDefinition(
        string displayName,
        StreamingPlatform platform,
        string identifier,
        EmbedAddresses addresses)
    {
        DisplayName = displayName;
        Platform = platform;
        Identifier = identifier;
        Addresses = addresses;
    }

    /// <summary>Obtient le nom visible de la colonne.</summary>
    internal string DisplayName { get; }

    /// <summary>Obtient la plateforme de diffusion.</summary>
    internal StreamingPlatform Platform { get; }

    /// <summary>Obtient l’identifiant public de la chaîne ou de la vidéo.</summary>
    internal string Identifier { get; }

    /// <summary>Obtient les adresses sécurisées du chat et du lecteur.</summary>
    internal EmbedAddresses Addresses { get; }
}
