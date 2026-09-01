namespace BouMultiChat.Core;

/// <summary>
/// Valide les adresses chargées dans les vues web isolées de l’application.
/// </summary>
public static class EmbedUrlValidator
{
    private const int MaximumUrlLength = 2_048;

    private static readonly Dictionary<StreamingPlatform, HashSet<string>> AllowedHosts =
        new Dictionary<StreamingPlatform, HashSet<string>>
        {
            [StreamingPlatform.Twitch] = new(StringComparer.OrdinalIgnoreCase)
            {
                "twitch.tv", "www.twitch.tv", "player.twitch.tv"
            },
            [StreamingPlatform.YouTube] = new(StringComparer.OrdinalIgnoreCase)
            {
                "youtube.com", "www.youtube.com", "youtube-nocookie.com", "www.youtube-nocookie.com"
            },
            [StreamingPlatform.Kick] = new(StringComparer.OrdinalIgnoreCase)
            {
                "kick.com", "www.kick.com", "player.kick.com"
            },
            [StreamingPlatform.TikTok] = new(StringComparer.OrdinalIgnoreCase)
            {
                "tiktok.com", "www.tiktok.com"
            }
        };

    /// <summary>
    /// Vérifie qu’une adresse absolue respecte la politique de navigation de la plateforme demandée.
    /// </summary>
    /// <param name="address">Adresse non fiable reçue de la configuration ou d’un connecteur.</param>
    /// <param name="platform">Plateforme qui doit être propriétaire de l’adresse.</param>
    /// <param name="failureReason">Motif français du refus, ou chaîne vide si l’adresse est valide.</param>
    /// <returns><see langword="true"/> lorsque l’adresse peut être chargée dans une vue web.</returns>
    public static bool TryValidate(string? address, StreamingPlatform platform, out string failureReason)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length > MaximumUrlLength)
        {
            failureReason = "L’adresse est vide ou dépasse la taille autorisée.";
            return false;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? uri))
        {
            failureReason = "L’adresse n’est pas une URL absolue valide.";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failureReason = "Seules les adresses HTTPS sont autorisées.";
            return false;
        }

        if (!uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
        {
            failureReason = "Les ports personnalisés et identifiants intégrés sont interdits.";
            return false;
        }

        if (!AllowedHosts[platform].Contains(uri.IdnHost))
        {
            failureReason = "Le domaine n’est pas autorisé pour cette plateforme.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }
}
