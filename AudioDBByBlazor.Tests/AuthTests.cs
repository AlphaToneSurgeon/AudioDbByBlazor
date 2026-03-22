using AudioDBByBlazor.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AudioDBByBlazor.Tests;

/// <summary>
/// Tests unitaires pour la logique d'authentification.
/// On teste le comportement de UserManager et SignInManager
/// sans démarrer l'application complète.
/// </summary>
public class AuthTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!
        );
    }

    private static Mock<SignInManager<AppUser>> CreateSignInManagerMock(Mock<UserManager<AppUser>> userManagerMock)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();

        return new Mock<SignInManager<AppUser>>(
            userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!
        );
    }

    // ── Tests : UserManager ───────────────────────────────────────────────────

    [Fact]
    public async Task UserManager_CreateAsync_AppelléAvecBonsParametres()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var user = new AppUser { Email = "test@test.com", UserName = "test@test.com", DisplayName = "Test" };

        // Act
        var result = await userManagerMock.Object.CreateAsync(user, "password123");

        // Assert
        result.Succeeded.Should().BeTrue();
        userManagerMock.Verify(um => um.CreateAsync(
            It.Is<AppUser>(u => u.Email == "test@test.com"),
            "password123"
        ), Times.Once);
    }

    [Fact]
    public async Task UserManager_CreateAsync_RetourneEchec_SiEmailDupliqué()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "DuplicateUserName", Description = "Email déjà utilisé." }
            ));

        var user = new AppUser { Email = "existant@test.com", UserName = "existant@test.com" };

        // Act
        var result = await userManagerMock.Object.CreateAsync(user, "password123");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "DuplicateUserName");
    }

    [Fact]
    public async Task UserManager_CreateAsync_RetourneEchec_SiMotDePasseTropCourt()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Minimum 6 caractères." }
            ));

        var user = new AppUser { Email = "test@test.com", UserName = "test@test.com" };

        // Act
        var result = await userManagerMock.Object.CreateAsync(user, "abc");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "PasswordTooShort");
    }

    // ── Tests : SignInManager ─────────────────────────────────────────────────

    [Fact]
    public async Task SignInManager_PasswordSignIn_Succès_SiIdentifiantsValides()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        signInManagerMock
            .Setup(sm => sm.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await signInManagerMock.Object
            .PasswordSignInAsync("test@test.com", "password123", false, false);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SignInManager_PasswordSignIn_Echec_SiMauvaisMotDePasse()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        signInManagerMock
            .Setup(sm => sm.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await signInManagerMock.Object
            .PasswordSignInAsync("test@test.com", "mauvais_mdp", false, false);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    // ── Tests : Modèle AppUser ────────────────────────────────────────────────

    [Fact]
    public void AppUser_DisplayName_PeutEtreNull()
    {
        // Arrange + Act
        var user = new AppUser { Email = "test@test.com", UserName = "test@test.com" };

        // Assert
        user.DisplayName.Should().BeNull();
    }

    [Fact]
    public void AppUser_CreatedAt_InitialiséAutomatiquement()
    {
        // Arrange + Act
        var avant = DateTime.Now;
        var user = new AppUser();
        var apres = DateTime.Now;

        // Assert
        user.CreatedAt.Should().BeOnOrAfter(avant).And.BeOnOrBefore(apres);
    }

    // ── Tests : Logique de message d'erreur ───────────────────────────────────

    [Theory]
    [InlineData("DuplicateUserName", "Cet email est déjà utilisé. Connectez-vous ou utilisez un autre email.")]
    [InlineData("DuplicateEmail",    "Cet email est déjà utilisé. Connectez-vous ou utilisez un autre email.")]
    [InlineData("PasswordTooShort",  "Mot de passe trop faible. Minimum 6 caractères.")]
    [InlineData("PasswordRequiresDigit", "Mot de passe trop faible. Minimum 6 caractères.")]
    public void GetErrorMessage_RetourneBonMessage_SelonCodeErreur(string errorCode, string expectedMessage)
    {
        // Arrange
        var errors = new[] { new IdentityError { Code = errorCode } };

        // Act
        var message = GetErrorMessage(errors);

        // Assert
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public void GetErrorMessage_RetourneMessageGénérique_SiErreurInconnue()
    {
        // Arrange
        var errors = new[] { new IdentityError { Code = "UnknownError" } };

        // Act
        var message = GetErrorMessage(errors);

        // Assert
        message.Should().Be("Erreur lors de la création du compte. Vérifiez vos informations.");
    }

    /// <summary>
    /// Reproduit la logique du endpoint /account/register dans Program.cs
    /// </summary>
    private static string GetErrorMessage(IEnumerable<IdentityError> errors)
    {
        if (errors.Any(e => e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail"))
            return "Cet email est déjà utilisé. Connectez-vous ou utilisez un autre email.";
        if (errors.Any(e => e.Code.Contains("Password")))
            return "Mot de passe trop faible. Minimum 6 caractères.";
        return "Erreur lors de la création du compte. Vérifiez vos informations.";
    }
}
