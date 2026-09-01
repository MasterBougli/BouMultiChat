using BouMultiChat.Core;

namespace BouMultiChat.Tests;

/// <summary>
/// Vérifie la frontière de confiance appliquée aux navigations des chats et lecteurs.
/// </summary>
public sealed class EmbedUrlValidatorTests
{
    /// <summary>
    /// Confirme que seules les adresses officielles attendues sont acceptées.
    /// </summary>
    /// <param name="address">Adresse officielle à contrôler.</param>
    /// <param name="platform">Plateforme propriétaire de l’adresse.</param>
    [Theory]
    [InlineData("https://www.twitch.tv/embed/demo/chat", StreamingPlatform.Twitch)]
    [InlineData("https://www.youtube-nocookie.com/embed/video", StreamingPlatform.YouTube)]
    [InlineData("https://player.kick.com/demo", StreamingPlatform.Kick)]
    [InlineData("https://www.tiktok.com/@demo/live", StreamingPlatform.TikTok)]
    public void TryValidateAccepteLesDomainesOfficiels(string address, StreamingPlatform platform)
    {
        bool accepted = EmbedUrlValidator.TryValidate(address, platform, out string reason);

        Assert.True(accepted, reason);
    }

    /// <summary>
    /// Confirme que les formes d’injection et de contournement courantes sont refusées.
    /// </summary>
    /// <param name="address">Adresse hostile ou ambiguë à contrôler.</param>
    /// <param name="platform">Plateforme annoncée par l’appelant.</param>
    [Theory]
    [InlineData("javascript:alert(1)", StreamingPlatform.Twitch)]
    [InlineData("http://www.twitch.tv/embed/demo/chat", StreamingPlatform.Twitch)]
    [InlineData("https://twitch.tv.evil.example/chat", StreamingPlatform.Twitch)]
    [InlineData("https://twitch.tv@evil.example/chat", StreamingPlatform.Twitch)]
    [InlineData("https://www.youtube.com:444/embed/video", StreamingPlatform.YouTube)]
    [InlineData("https://www.twitch.tv/embed/demo/chat", StreamingPlatform.YouTube)]
    public void TryValidateRefuseLesAdressesHostiles(string address, StreamingPlatform platform)
    {
        bool accepted = EmbedUrlValidator.TryValidate(address, platform, out string reason);

        Assert.False(accepted);
        Assert.NotEmpty(reason);
    }

    /// <summary>
    /// Confirme qu’une entrée absente est refusée avant toute navigation.
    /// </summary>
    /// <param name="address">Entrée vide à contrôler.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidateRefuseLesEntreesVides(string? address)
    {
        bool accepted = EmbedUrlValidator.TryValidate(address, StreamingPlatform.Twitch, out string reason);

        Assert.False(accepted);
        Assert.NotEmpty(reason);
    }
}
