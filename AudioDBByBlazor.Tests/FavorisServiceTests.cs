using AudioDBByBlazor.Models;
using AudioDBByBlazor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace AudioDBByBlazor.Tests;

/// <summary>
/// Tests unitaires pour FavorisService.
/// On utilise un dossier temporaire pour ne pas polluer les vraies données.
/// </summary>
public class FavorisServiceTests : IDisposable
{
    private readonly FavorisService _service;
    private readonly string _tempPath;
    private readonly string _userId = "user-test-123";

    public FavorisServiceTests()
    {
        // Crée un dossier temporaire unique pour chaque test
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_tempPath);

        _service = new FavorisService(mockEnv.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Favori CreateFavori(string artistId = "111239", string artistName = "Coldplay") => new()
    {
        IdArtist = artistId,
        StrArtist = artistName,
        StrGenre = "Alternative Rock",
        StrCountry = "United Kingdom",
        StrArtistThumb = "https://example.com/thumb.jpg"
    };

    // ── Tests : GetFavorisAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetFavorisAsync_RetourneListeVide_SiAucunFichier()
    {
        // Arrange : aucun fichier JSON n'existe pour cet utilisateur

        // Act
        var result = await _service.GetFavorisAsync(_userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFavorisAsync_RetourneLeFavori_AprèsAjout()
    {
        // Arrange
        var favori = CreateFavori();
        await _service.AddFavoriAsync(_userId, favori);

        // Act
        var result = await _service.GetFavorisAsync(_userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].StrArtist.Should().Be("Coldplay");
        result[0].IdArtist.Should().Be("111239");
    }

    // ── Tests : AddFavoriAsync ────────────────────────────────────────────────

    [Fact]
    public async Task AddFavoriAsync_RetourneTrue_SiNouveauFavori()
    {
        // Arrange
        var favori = CreateFavori();

        // Act
        var result = await _service.AddFavoriAsync(_userId, favori);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddFavoriAsync_RetourneFalse_SiDejaEnFavori()
    {
        // Arrange : on ajoute une première fois
        var favori = CreateFavori();
        await _service.AddFavoriAsync(_userId, favori);

        // Act : on tente d'ajouter le même artiste
        var result = await _service.AddFavoriAsync(_userId, CreateFavori());

        // Assert : refusé car doublon
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddFavoriAsync_AssigneUserId_Automatiquement()
    {
        // Arrange
        var favori = CreateFavori();

        // Act
        await _service.AddFavoriAsync(_userId, favori);
        var favoris = await _service.GetFavorisAsync(_userId);

        // Assert : le UserId a bien été assigné par le service
        favoris[0].UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task AddFavoriAsync_PeutAjouter_PlusieursArtistes()
    {
        // Arrange
        var favori1 = CreateFavori("111239", "Coldplay");
        var favori2 = CreateFavori("111240", "Radiohead");
        var favori3 = CreateFavori("111241", "Muse");

        // Act
        await _service.AddFavoriAsync(_userId, favori1);
        await _service.AddFavoriAsync(_userId, favori2);
        await _service.AddFavoriAsync(_userId, favori3);

        var result = await _service.GetFavorisAsync(_userId);

        // Assert
        result.Should().HaveCount(3);
        result.Select(f => f.StrArtist).Should().Contain(new[] { "Coldplay", "Radiohead", "Muse" });
    }

    // ── Tests : UpdateFavoriAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateFavoriAsync_ModifieLaNote_Correctement()
    {
        // Arrange
        var favori = CreateFavori();
        await _service.AddFavoriAsync(_userId, favori);
        var favoris = await _service.GetFavorisAsync(_userId);
        var favoriSauvegarde = favoris[0];

        // Act
        favoriSauvegarde.NotePersonnelle = "Mon groupe préféré !";
        favoriSauvegarde.NoteSur10 = 9;
        favoriSauvegarde.TagsPersonnels = "rock, live, concert";
        await _service.UpdateFavoriAsync(_userId, favoriSauvegarde);

        // Assert
        var result = await _service.GetFavorisAsync(_userId);
        result[0].NotePersonnelle.Should().Be("Mon groupe préféré !");
        result[0].NoteSur10.Should().Be(9);
        result[0].TagsPersonnels.Should().Be("rock, live, concert");
    }

    [Fact]
    public async Task UpdateFavoriAsync_MetsAJourDateModification()
    {
        // Arrange
        var favori = CreateFavori();
        await _service.AddFavoriAsync(_userId, favori);
        var favoris = await _service.GetFavorisAsync(_userId);
        var favoriSauvegarde = favoris[0];
        var dateAvant = favoriSauvegarde.DateModification;

        // Petite attente pour s'assurer que la date change
        await Task.Delay(10);

        // Act
        favoriSauvegarde.NotePersonnelle = "Modifié";
        await _service.UpdateFavoriAsync(_userId, favoriSauvegarde);

        // Assert
        var result = await _service.GetFavorisAsync(_userId);
        result[0].DateModification.Should().BeAfter(dateAvant);
    }

    [Fact]
    public async Task UpdateFavoriAsync_NeModifiePasLesAutresFavoris()
    {
        // Arrange : deux favoris
        var favori1 = CreateFavori("111239", "Coldplay");
        var favori2 = CreateFavori("111240", "Radiohead");
        await _service.AddFavoriAsync(_userId, favori1);
        await _service.AddFavoriAsync(_userId, favori2);

        var favoris = await _service.GetFavorisAsync(_userId);
        var aModifier = favoris.First(f => f.StrArtist == "Coldplay");

        // Act : on modifie seulement Coldplay
        aModifier.NoteSur10 = 10;
        await _service.UpdateFavoriAsync(_userId, aModifier);

        // Assert : Radiohead n'est pas touché
        var result = await _service.GetFavorisAsync(_userId);
        result.First(f => f.StrArtist == "Radiohead").NoteSur10.Should().BeNull();
        result.First(f => f.StrArtist == "Coldplay").NoteSur10.Should().Be(10);
    }

    // ── Tests : DeleteFavoriAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteFavoriAsync_SupprimeLeFavori_Correctement()
    {
        // Arrange
        var favori = CreateFavori();
        await _service.AddFavoriAsync(_userId, favori);
        var favoris = await _service.GetFavorisAsync(_userId);
        var id = favoris[0].Id;

        // Act
        await _service.DeleteFavoriAsync(_userId, id);

        // Assert
        var result = await _service.GetFavorisAsync(_userId);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteFavoriAsync_NeSupprimePas_LesAutresFavoris()
    {
        // Arrange : deux favoris
        await _service.AddFavoriAsync(_userId, CreateFavori("111239", "Coldplay"));
        await _service.AddFavoriAsync(_userId, CreateFavori("111240", "Radiohead"));

        var favoris = await _service.GetFavorisAsync(_userId);
        var idColdplay = favoris.First(f => f.StrArtist == "Coldplay").Id;

        // Act : on supprime Coldplay
        await _service.DeleteFavoriAsync(_userId, idColdplay);

        // Assert : Radiohead est toujours là
        var result = await _service.GetFavorisAsync(_userId);
        result.Should().HaveCount(1);
        result[0].StrArtist.Should().Be("Radiohead");
    }

    [Fact]
    public async Task DeleteFavoriAsync_NeThrowPas_SiIdInexistant()
    {
        // Arrange : aucun favori

        // Act + Assert : ne doit pas lever d'exception
        var act = async () => await _service.DeleteFavoriAsync(_userId, Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    // ── Tests : IsFavoriAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task IsFavoriAsync_RetourneTrue_SiArtistEnFavori()
    {
        // Arrange
        await _service.AddFavoriAsync(_userId, CreateFavori("111239"));

        // Act
        var result = await _service.IsFavoriAsync(_userId, "111239");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFavoriAsync_RetourneFalse_SiArtistPasEnFavori()
    {
        // Act
        var result = await _service.IsFavoriAsync(_userId, "111239");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFavoriAsync_EstIsolePar_Utilisateur()
    {
        // Arrange : user1 ajoute un favori
        await _service.AddFavoriAsync("user-1", CreateFavori("111239"));

        // Act : user2 vérifie — ne doit pas voir le favori de user1
        var result = await _service.IsFavoriAsync("user-2", "111239");

        // Assert
        result.Should().BeFalse();
    }

    // ── Nettoyage ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        // Supprime le dossier temporaire après chaque test
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, recursive: true);
    }
}
