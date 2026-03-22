using AudioDBByBlazor.Services;
using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace AudioDBByBlazor.Tests;

/// <summary>
/// Tests unitaires pour AudioDbService.
/// On mocke HttpMessageHandler pour simuler les réponses de TheAudioDB
/// sans faire de vraies requêtes réseau.
/// </summary>
public class AudioDbServiceTests
{
    // ── Helper : crée un HttpClient avec une réponse simulée ─────────────────

    private static AudioDbService CreateService(string jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        return new AudioDbService(httpClient);
    }

    // ── Tests : SearchArtistsAsync ────────────────────────────────────────────

    [Fact]
    public async Task SearchArtistsAsync_RetourneArtistes_SiRéponseValide()
    {
        // Arrange : on simule la réponse JSON de TheAudioDB
        var json = JsonSerializer.Serialize(new
        {
            artists = new[]
            {
                new { idArtist = "111239", strArtist = "Coldplay", strGenre = "Alternative Rock", strCountry = "United Kingdom", strArtistThumb = (string?)null },
                new { idArtist = "111240", strArtist = "Radiohead", strGenre = "Alternative Rock", strCountry = "United Kingdom", strArtistThumb = (string?)null }
            }
        });

        var service = CreateService(json);

        // Act
        var result = await service.SearchArtistsAsync("cold");

        // Assert
        result.Should().HaveCount(2);
        result[0].StrArtist.Should().Be("Coldplay");
        result[0].IdArtist.Should().Be("111239");
        result[1].StrArtist.Should().Be("Radiohead");
    }

    [Fact]
    public async Task SearchArtistsAsync_RetourneListeVide_SiAucunRésultat()
    {
        // Arrange : TheAudioDB retourne null quand aucun artiste trouvé
        var json = JsonSerializer.Serialize(new { artists = (object?)null });
        var service = CreateService(json);

        // Act
        var result = await service.SearchArtistsAsync("xyzinexistant");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchArtistsAsync_RetourneListeVide_SiErreurHTTP()
    {
        // Arrange : l'API retourne une erreur 500
        var service = CreateService("{}", HttpStatusCode.InternalServerError);

        // Act
        var result = await service.SearchArtistsAsync("coldplay");

        // Assert : pas d'exception, liste vide
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchArtistsAsync_RetourneListeVide_SiJsonInvalide()
    {
        // Arrange : réponse JSON malformée
        var service = CreateService("{ invalid json {{");

        // Act
        var result = await service.SearchArtistsAsync("coldplay");

        // Assert : pas d'exception, liste vide
        result.Should().BeEmpty();
    }

    // ── Tests : GetArtistByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetArtistByIdAsync_RetourneArtiste_SiIdValide()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            artists = new[]
            {
                new
                {
                    idArtist = "111239",
                    strArtist = "Coldplay",
                    strGenre = "Alternative Rock",
                    strCountry = "United Kingdom",
                    strBiographyEN = "Coldplay is a British rock band.",
                    intFormedYear = "1996",
                    strArtistThumb = (string?)null
                }
            }
        });

        var service = CreateService(json);

        // Act
        var result = await service.GetArtistByIdAsync("111239");

        // Assert
        result.Should().NotBeNull();
        result!.StrArtist.Should().Be("Coldplay");
        result.StrGenre.Should().Be("Alternative Rock");
        result.IntFormedYear.Should().Be("1996");
    }

    [Fact]
    public async Task GetArtistByIdAsync_RetourneNull_SiArtistePasTrouvé()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new { artists = (object?)null });
        var service = CreateService(json);

        // Act
        var result = await service.GetArtistByIdAsync("999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetArtistByIdAsync_RetourneNull_SiErreurRéseau()
    {
        // Arrange : simule une erreur réseau
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Réseau indisponible"));

        var service = new AudioDbService(new HttpClient(handlerMock.Object));

        // Act
        var result = await service.GetArtistByIdAsync("111239");

        // Assert : pas d'exception, null retourné
        result.Should().BeNull();
    }

    // ── Tests : GetAlbumsByArtistAsync ────────────────────────────────────────

    [Fact]
    public async Task GetAlbumsByArtistAsync_RetourneAlbums_SiRéponseValide()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            album = new[]
            {
                new { idAlbum = "2115888", idArtist = "111239", strAlbum = "Parachutes", intYearReleased = "2000", strAlbumThumb = (string?)null },
                new { idAlbum = "2115889", idArtist = "111239", strAlbum = "A Rush of Blood to the Head", intYearReleased = "2002", strAlbumThumb = (string?)null }
            }
        });

        var service = CreateService(json);

        // Act
        var result = await service.GetAlbumsByArtistAsync("111239");

        // Assert
        result.Should().HaveCount(2);
        result[0].StrAlbum.Should().Be("Parachutes");
        result[1].StrAlbum.Should().Be("A Rush of Blood to the Head");
    }

    [Fact]
    public async Task GetAlbumsByArtistAsync_RetourneListeVide_SiAucunAlbum()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new { album = (object?)null });
        var service = CreateService(json);

        // Act
        var result = await service.GetAlbumsByArtistAsync("111239");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAlbumsByArtistAsync_RetourneListeVide_SiErreurHTTP()
    {
        // Arrange
        var service = CreateService("{}", HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await service.GetAlbumsByArtistAsync("111239");

        // Assert
        result.Should().BeEmpty();
    }
}
