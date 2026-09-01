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

    /// <summary>
    /// Produit la représentation minimale enregistrée sur le disque local.
    /// </summary>
    /// <returns>Données publiques nécessaires à la restauration.</returns>
    internal SavedStreamColumn ToSavedColumn()
    {
        return new SavedStreamColumn
        {
            DisplayName = DisplayName,
            Platform = Platform,
            Identifier = Identifier
        };
    }

    /// <summary>
    /// Valide une entrée rechargée avant de la transformer en définition utilisable.
    /// </summary>
    /// <param name="saved">Entrée provenant du fichier local non fiable.</param>
    /// <param name="definition">Définition sûre créée en cas de succès.</param>
    /// <returns><see langword="true"/> lorsque toutes les données sont valides.</returns>
    internal static bool TryRestore(SavedStreamColumn? saved, out StreamColumnDefinition? definition)
    {
        definition = null;
        if (saved is null
            || saved.DisplayName is null
            || saved.Identifier is null
            || saved.DisplayName.Length is < 1 or > 40)
        {
            return false;
        }

        string displayName = saved.DisplayName.Trim();
        string identifier = saved.Identifier.Trim();
        if (displayName.Length is < 1 or > 40
            || !EmbedUrlFactory.TryCreate(saved.Platform, identifier, out EmbedAddresses? addresses, out _))
        {
            return false;
        }

        definition = new StreamColumnDefinition(displayName, saved.Platform, identifier, addresses!);
        return true;
    }
}
