using System.Text.Json;

namespace BouMultiChat.Core;

/// <summary>
/// Sauvegarde et recharge les colonnes dans un fichier JSON local de taille bornée.
/// </summary>
public sealed class ColumnSettingsStore
{
    private const int MaximumFileSize = 65_536;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string filePath;

    /// <summary>
    /// Initialise le stockage avec le chemin local fourni.
    /// </summary>
    /// <param name="filePath">Chemin absolu du fichier de configuration.</param>
    public ColumnSettingsStore(string filePath)
    {
        this.filePath = filePath;
    }

    /// <summary>
    /// Crée le stockage dans le dossier de données locales de l’utilisateur Windows.
    /// </summary>
    /// <returns>Stockage prêt à lire et écrire les colonnes.</returns>
    public static ColumnSettingsStore CreateDefault()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BouMultiChat");

        return new ColumnSettingsStore(Path.Combine(folder, "columns.json"));
    }

    /// <summary>
    /// Recharge les entrées JSON sans considérer leur contenu comme fiable.
    /// </summary>
    /// <returns>Entrées désérialisées, ou collection vide si le fichier est absent ou invalide.</returns>
    public IReadOnlyList<SavedStreamColumn> Load()
    {
        try
        {
            FileInfo file = new(filePath);
            if (!file.Exists || file.Length > MaximumFileSize)
            {
                return Array.Empty<SavedStreamColumn>();
            }

            using FileStream stream = File.OpenRead(filePath);
            List<SavedStreamColumn>? columns =
                JsonSerializer.Deserialize<List<SavedStreamColumn>>(stream, SerializerOptions);
            return columns is null ? Array.Empty<SavedStreamColumn>() : columns;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return Array.Empty<SavedStreamColumn>();
        }
    }

    /// <summary>
    /// Écrit atomiquement la configuration afin de ne pas conserver un fichier partiel.
    /// </summary>
    /// <param name="columns">Colonnes déjà validées à enregistrer.</param>
    public void Save(IEnumerable<SavedStreamColumn> columns)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("Le chemin de sauvegarde ne contient aucun dossier.");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = filePath + ".tmp";

        using (FileStream stream = File.Create(temporaryPath))
        {
            JsonSerializer.Serialize(stream, columns, SerializerOptions);
            stream.Flush(true);
        }

        File.Move(temporaryPath, filePath, true);
    }
}
