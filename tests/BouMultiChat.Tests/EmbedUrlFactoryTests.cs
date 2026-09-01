using BouMultiChat.Core;

namespace BouMultiChat.Tests;

/// <summary>
/// Vérifie la construction sûre des adresses officielles de diffusion.
/// </summary>
public sealed class EmbedUrlFactoryTests
{
    /// <summary>
    /// Confirme qu’un nom Twitch valide produit deux adresses Twitch autorisées.
    /// </summary>
    [Fact]
    public void TryCreateConstruitUneColonneTwitchValide()
    {
        bool created = EmbedUrlFactory.TryCreate(
            StreamingPlatform.Twitch,
            "OpenAI_42",
            out EmbedAddresses? addresses,
            out string reason);

        Assert.True(created, reason);
        Assert.NotNull(addresses);
        Assert.Equal("www.twitch.tv", addresses.Chat.Host);
        Assert.Equal("player.twitch.tv", addresses.Video.Host);
    }

    /// <summary>
    /// Confirme qu’un identifiant YouTube valide produit deux adresses YouTube autorisées.
    /// </summary>
    [Fact]
    public void TryCreateConstruitUneColonneYouTubeValide()
    {
        bool created = EmbedUrlFactory.TryCreate(
            StreamingPlatform.YouTube,
            "dQw4w9WgXcQ",
            out EmbedAddresses? addresses,
            out string reason);

        Assert.True(created, reason);
        Assert.NotNull(addresses);
        Assert.Equal("www.youtube.com", addresses.Chat.Host);
        Assert.Equal("www.youtube-nocookie.com", addresses.Video.Host);
    }

    /// <summary>
    /// Confirme que les identifiants contenant une charge hostile ou une taille incorrecte sont refusés.
    /// </summary>
    /// <param name="platform">Plateforme annoncée.</param>
    /// <param name="identifier">Identifiant hostile à refuser.</param>
    [Theory]
    [InlineData(StreamingPlatform.Twitch, "demo/../../evil")]
    [InlineData(StreamingPlatform.Twitch, "<script>alert(1)</script>")]
    [InlineData(StreamingPlatform.YouTube, "javascript:")]
    [InlineData(StreamingPlatform.YouTube, "trop-court")]
    [InlineData(StreamingPlatform.Kick, "demo")]
    [InlineData(StreamingPlatform.TikTok, "demo")]
    public void TryCreateRefuseLesIdentifiantsHostiles(StreamingPlatform platform, string identifier)
    {
        bool created = EmbedUrlFactory.TryCreate(platform, identifier, out EmbedAddresses? addresses, out string reason);

        Assert.False(created);
        Assert.Null(addresses);
        Assert.NotEmpty(reason);
    }
}
