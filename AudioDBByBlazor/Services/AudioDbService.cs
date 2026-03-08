using AudioDBByBlazor.Models;
using System.Text.Json;

namespace AudioDBByBlazor.Services;

/// <summary>
/// Service responsable des appels à l'API TheAudioDB.
/// Fournit des méthodes pour rechercher des artistes et récupérer leurs albums.
/// </summary>
public class AudioDbService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://www.theaudiodb.com/api/v1/json/2";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AudioDbService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Recherche des artistes par nom via TheAudioDB.
    /// </summary>
    /// <param name="name">Nom de l'artiste à rechercher</param>
    /// <returns>Liste des artistes correspondants, ou liste vide si aucun résultat</returns>
    public async Task<List<Artist>> SearchArtistsAsync(string name)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ArtistSearchResult>(
                $"{BaseUrl}/search.php?s={Uri.EscapeDataString(name)}",
                _jsonOptions
            );
            return response?.Artists ?? new List<Artist>();
        }
        catch
        {
            return new List<Artist>();
        }
    }

    /// <summary>
    /// Récupère les détails complets d'un artiste par son ID.
    /// </summary>
    /// <param name="id">Identifiant TheAudioDB de l'artiste</param>
    /// <returns>L'artiste trouvé, ou null si inexistant</returns>
    public async Task<Artist?> GetArtistByIdAsync(string id)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ArtistSearchResult>(
                $"{BaseUrl}/artist.php?i={id}",
                _jsonOptions
            );
            return response?.Artists?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Récupère la liste des albums d'un artiste.
    /// </summary>
    /// <param name="artistId">Identifiant TheAudioDB de l'artiste</param>
    /// <returns>Liste des albums de l'artiste</returns>
    public async Task<List<Album>> GetAlbumsByArtistAsync(string artistId)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<AlbumSearchResult>(
                $"{BaseUrl}/album.php?i={artistId}",
                _jsonOptions
            );
            return response?.Album ?? new List<Album>();
        }
        catch
        {
            return new List<Album>();
        }
    }

    // Classes internes pour la désérialisation JSON
    private class ArtistSearchResult
    {
        public List<Artist>? Artists { get; set; }
    }

    private class AlbumSearchResult
    {
        public List<Album>? Album { get; set; }
    }
}
