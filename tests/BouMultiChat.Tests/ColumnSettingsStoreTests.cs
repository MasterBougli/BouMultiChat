using BouMultiChat.Core;

namespace BouMultiChat.Tests;

/// <summary>
/// Vérifie la persistance locale bornée des colonnes.
/// </summary>
public sealed class ColumnSettingsStoreTests
{
    /// <summary>
    /// Confirme qu’une liste enregistrée peut être rechargée sans perte.
    /// </summary>
    [Fact]
    public void SavePuisLoadRestaurentLesColonnes()
    {
        string directory = Path.Combine(Path.GetTempPath(), "BouMultiChatTests", Guid.NewGuid().ToString("N"));
        try
        {
            ColumnSettingsStore store = new(Path.Combine(directory, "columns.json"));
            store.Save(
            [
                new SavedStreamColumn
                {
                    DisplayName = "Direct principal",
                    Platform = StreamingPlatform.Twitch,
                    Identifier = "demo"
                }
            ]);

            SavedStreamColumn restored = Assert.Single(store.Load());
            Assert.Equal("Direct principal", restored.DisplayName);
            Assert.Equal(StreamingPlatform.Twitch, restored.Platform);
            Assert.Equal("demo", restored.Identifier);
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
    /// Confirme qu’un fichier altéré est ignoré au lieu de faire échouer l’application.
    /// </summary>
    [Fact]
    public void LoadIgnoreUnFichierJsonAltere()
    {
        string directory = Path.Combine(Path.GetTempPath(), "BouMultiChatTests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, "columns.json");
            File.WriteAllText(filePath, "{contenu invalide");

            ColumnSettingsStore store = new(filePath);

            Assert.Empty(store.Load());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
