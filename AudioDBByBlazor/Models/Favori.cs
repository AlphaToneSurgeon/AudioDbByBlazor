namespace AudioDBByBlazor.Models;

public class Favori
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;

    // Données originales de l'API
    public string IdArtist { get; set; } = string.Empty;
    public string StrArtist { get; set; } = string.Empty;
    public string? StrArtistThumb { get; set; }
    public string? StrGenre { get; set; }
    public string? StrCountry { get; set; }

    // Données personnalisées par l'utilisateur
    public string? NotePersonnelle { get; set; }
    public int? NoteSur10 { get; set; }
    public string? TagsPersonnels { get; set; }

    public DateTime DateAjout { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
}
