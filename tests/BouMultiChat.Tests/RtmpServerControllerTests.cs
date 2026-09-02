using BouMultiChat.Core;

namespace BouMultiChat.Tests;

/// <summary>
/// Vérifie la frontière de sécurité du contrôleur BouVideoServ.
/// </summary>
public sealed class RtmpServerControllerTests
{
    /// <summary>
    /// Confirme que seul un fichier existant portant le nom attendu est accepté.
    /// </summary>
    [Fact]
    public void TryNormalizeExecutablePathAccepteUniquementBouVideoServ()
    {
        string directory = Path.Combine(Path.GetTempPath(), "BouMultiChatTests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            string validPath = Path.Combine(directory, "bouvideoserv.exe");
            string invalidPath = Path.Combine(directory, "autre.exe");
            File.WriteAllBytes(validPath, []);
            File.WriteAllBytes(invalidPath, []);

            bool valid = RtmpServerController.TryNormalizeExecutablePath(
                validPath,
                out string? normalizedPath,
                out string validReason);
            bool invalid = RtmpServerController.TryNormalizeExecutablePath(
                invalidPath,
                out _,
                out string invalidReason);

            Assert.True(valid, validReason);
            Assert.Equal(Path.GetFullPath(validPath), normalizedPath);
            Assert.False(invalid);
            Assert.NotEmpty(invalidReason);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>
    /// Confirme que l’état prêt exige simultanément l’API, MediaMTX et le port RTMP.
    /// </summary>
    [Fact]
    public void IsReadyExigeLesTroisSignaux()
    {
        RtmpServerStatus ready = new(true, true, true, true, "Prêt");
        RtmpServerStatus partial = new(true, false, true, true, "Partiel");

        Assert.True(ready.IsReady);
        Assert.False(partial.IsReady);
    }
}
