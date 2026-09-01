namespace BouMultiChat.Core;

/// <summary>
/// Représente les seules données utilisateur nécessaires pour restaurer une colonne.
/// </summary>
public sealed class SavedStreamColumn
{
    /// <summary>Obtient ou définit le nom visible de la colonne.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Obtient ou définit la plateforme enregistrée.</summary>
    public StreamingPlatform Platform { get; set; }

    /// <summary>Obtient ou définit l’identifiant public de la diffusion.</summary>
    public string Identifier { get; set; } = string.Empty;
}
