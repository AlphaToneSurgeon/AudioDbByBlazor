using AudioDBByBlazor.Models;
using System.Text.Json;

namespace AudioDBByBlazor.Services;

/// <summary>
/// Service de gestion des favoris utilisateur.
/// Les favoris sont stockés localement dans un fichier JSON par utilisateur.
/// </summary>
public class FavorisService
{
    private readonly string _dataPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FavorisService(IWebHostEnvironment env)
    {
        _dataPath = Path.Combine(env.ContentRootPath, "Data", "favoris");
        Directory.CreateDirectory(_dataPath);
    }

    private string GetFilePath(string userId) =>
        Path.Combine(_dataPath, $"{userId}.json");

    /// <summary>
    /// Récupère tous les favoris d'un utilisateur.
    /// </summary>
    public async Task<List<Favori>> GetFavorisAsync(string userId)
    {
        var path = GetFilePath(userId);
        if (!File.Exists(path)) return new List<Favori>();

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<Favori>>(json, _jsonOptions) ?? new List<Favori>();
    }

    /// <summary>
    /// Ajoute un artiste aux favoris de l'utilisateur.
    /// </summary>
    public async Task<bool> AddFavoriAsync(string userId, Favori favori)
    {
        var favoris = await GetFavorisAsync(userId);

        // Vérifier si déjà en favori
        if (favoris.Any(f => f.IdArtist == favori.IdArtist))
            return false;

        favori.UserId = userId;
        favoris.Add(favori);
        await SaveAsync(userId, favoris);
        return true;
    }

    /// <summary>
    /// Met à jour un favori existant (note, commentaire, tags).
    /// </summary>
    public async Task UpdateFavoriAsync(string userId, Favori updated)
    {
        var favoris = await GetFavorisAsync(userId);
        var index = favoris.FindIndex(f => f.Id == updated.Id);

        if (index >= 0)
        {
            updated.DateModification = DateTime.Now;
            favoris[index] = updated;
            await SaveAsync(userId, favoris);
        }
    }

    /// <summary>
    /// Supprime un favori par son ID.
    /// </summary>
    public async Task DeleteFavoriAsync(string userId, Guid favoriId)
    {
        var favoris = await GetFavorisAsync(userId);
        favoris.RemoveAll(f => f.Id == favoriId);
        await SaveAsync(userId, favoris);
    }

    /// <summary>
    /// Vérifie si un artiste est déjà dans les favoris de l'utilisateur.
    /// </summary>
    public async Task<bool> IsFavoriAsync(string userId, string artistId)
    {
        var favoris = await GetFavorisAsync(userId);
        return favoris.Any(f => f.IdArtist == artistId);
    }

    private async Task SaveAsync(string userId, List<Favori> favoris)
    {
        var json = JsonSerializer.Serialize(favoris, _jsonOptions);
        await File.WriteAllTextAsync(GetFilePath(userId), json);
    }
}
