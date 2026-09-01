using System.Text.RegularExpressions;

namespace BouMultiChat.Core;

/// <summary>
/// Construit les adresses officielles à partir d’identifiants publics strictement validés.
/// </summary>
public static partial class EmbedUrlFactory
{
    /// <summary>
    /// Construit les adresses du chat et du lecteur pour une plateforme prise en charge.
    /// </summary>
    /// <param name="platform">Plateforme de la diffusion.</param>
    /// <param name="identifier">Nom de chaîne ou identifiant public attendu par la plateforme.</param>
    /// <param name="addresses">Adresses validées lorsque la création réussit.</param>
    /// <param name="failureReason">Motif français du refus.</param>
    /// <returns><see langword="true"/> lorsque les deux adresses sont sûres et utilisables.</returns>
    public static bool TryCreate(
        StreamingPlatform platform,
        string? identifier,
        out EmbedAddresses? addresses,
        out string failureReason)
    {
        addresses = null;
        string normalizedIdentifier = identifier?.Trim() ?? string.Empty;

        string chatAddress;
        string videoAddress;

        switch (platform)
        {
            case StreamingPlatform.Twitch when TwitchChannelRegex().IsMatch(normalizedIdentifier):
                string twitchChannel = normalizedIdentifier.ToLowerInvariant();
                chatAddress = $"https://www.twitch.tv/embed/{twitchChannel}/chat?parent=localhost&darkpopout";
                videoAddress = $"https://player.twitch.tv/?channel={twitchChannel}&parent=localhost&muted=true";
                break;

            case StreamingPlatform.YouTube when YouTubeVideoRegex().IsMatch(normalizedIdentifier):
                chatAddress = $"https://www.youtube.com/live_chat?v={normalizedIdentifier}&embed_domain=localhost";
                videoAddress = $"https://www.youtube-nocookie.com/embed/{normalizedIdentifier}";
                break;

            case StreamingPlatform.Kick when ChannelNameRegex().IsMatch(normalizedIdentifier):
                string kickChannel = normalizedIdentifier.ToLowerInvariant();
                chatAddress = $"https://kick.com/{kickChannel}/chatroom";
                videoAddress = $"https://player.kick.com/{kickChannel}?muted=true&autoplay=false";
                break;

            case StreamingPlatform.Trovo when ChannelNameRegex().IsMatch(normalizedIdentifier):
                chatAddress = $"https://player.trovo.live/chat/{normalizedIdentifier}";
                videoAddress = $"https://player.trovo.live/embed/player?streamername={normalizedIdentifier}&muted=1&autoplay=0&hidefollow=1&hidesub=1";
                break;

            case StreamingPlatform.Twitch:
                failureReason = "Le nom Twitch doit contenir 1 à 25 lettres, chiffres ou traits de soulignement.";
                return false;

            case StreamingPlatform.YouTube:
                failureReason = "L’identifiant d’une vidéo YouTube doit contenir exactement 11 caractères autorisés.";
                return false;

            case StreamingPlatform.Kick:
            case StreamingPlatform.Trovo:
                failureReason = "Le nom de chaîne doit contenir 1 à 30 lettres, chiffres ou traits de soulignement.";
                return false;

            default:
                failureReason = "Cette plateforme ne possède pas encore de connecteur intégré fiable.";
                return false;
        }

        if (!EmbedUrlValidator.TryValidate(chatAddress, platform, out failureReason)
            || !EmbedUrlValidator.TryValidate(videoAddress, platform, out failureReason))
        {
            return false;
        }

        addresses = new EmbedAddresses(new Uri(chatAddress), new Uri(videoAddress));
        failureReason = string.Empty;
        return true;
    }

    /// <summary>
    /// Fournit l’expression régulière compilée des noms de chaîne Twitch autorisés.
    /// </summary>
    /// <returns>Expression régulière bornée et insensible à la culture.</returns>
    [GeneratedRegex("^[A-Za-z0-9_]{1,25}$", RegexOptions.CultureInvariant)]
    private static partial Regex TwitchChannelRegex();

    /// <summary>
    /// Fournit l’expression régulière compilée des identifiants vidéo YouTube autorisés.
    /// </summary>
    /// <returns>Expression régulière bornée et insensible à la culture.</returns>
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex YouTubeVideoRegex();

    /// <summary>
    /// Fournit l’expression régulière compilée des noms de chaîne Kick et Trovo autorisés.
    /// </summary>
    /// <returns>Expression régulière bornée et insensible à la culture.</returns>
    [GeneratedRegex("^[A-Za-z0-9_]{1,30}$", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelNameRegex();
}
