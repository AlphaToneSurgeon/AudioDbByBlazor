# 🔥 Problèmes rencontrés – Authentification Blazor & Solutions

## Contexte
Mise en place de l'authentification (Login / Register) dans une application
**Blazor Web App (.NET 8/9)** avec **ASP.NET Core Identity** et **SQLite**.

---

## Problème 1 — `KeyboardEventArgs` introuvable

### Erreur
```
error CS0246: Le nom de type ou d'espace de noms 'KeyboardEventArgs' est introuvable
```

### Cause
Le `using` pour les événements clavier de Blazor était manquant dans la page.

### Solution
Ajouter en haut de `Home.razor` :
```razor
@using Microsoft.AspNetCore.Components.Web
```

---

## Problème 2 — Services non reconnus après `@inject`

### Erreur
Le code n'était plus reconnu à partir de `@inject AudioDbService AudioDb`.

### Cause
Le fichier `_Imports.razor` était absent. Ce fichier est essentiel en Blazor —
il injecte automatiquement tous les `@using` dans toutes les pages du dossier.

### Solution
Créer `Components/_Imports.razor` avec tous les namespaces nécessaires :
```razor
@using AudioDBByBlazor.Models
@using AudioDBByBlazor.Services
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
...
```

---

## Problème 3 — Guillemets dans les attributs Razor `@onclick`

### Erreur
```
error RZ1030: TagHelper attributes must be well-formed.
error CS1056: Caractère inattendu '$'
```

### Cause
En Razor, on ne peut pas mettre de `"` à l'intérieur d'un attribut déjà délimité
par `"`. Exemples problématiques :
```razor
@onclick="() => Navigation.NavigateTo($"/artist/{f.IdArtist}")"
class="btn-fav @(_isFavori ? "active" : "")"
```

### Solution
Extraire la logique dans des méthodes C# dans le bloc `@code` :
```razor
@* Avant *@
<button @onclick="() => Navigation.NavigateTo($"/artist/{f.IdArtist}")">

@* Après *@
<button @onclick="() => GoToArtist(f.IdArtist)">

@code {
    private void GoToArtist(string id) => Navigation.NavigateTo("/artist/" + id);
}
```
> 💡 Règle à retenir : jamais de `"` dans un attribut HTML en Razor.
> Toujours extraire en méthode dans `@code`.

---

## Problème 4 — `InteractiveServer` introuvable

### Erreur
```
error CS0103: Le nom 'InteractiveServer' n'existe pas dans le contexte actuel
```

### Cause
La syntaxe raccourcie `@rendermode InteractiveServer` ne fonctionne pas sans
import spécifique.

### Solution
Utiliser la syntaxe complète :
```razor
@rendermode @(new InteractiveServerRenderMode())
```

---

## Problème 5 — Le bouton Login/Register ne fait rien (freeze)

### Symptôme
Le bouton passe à "Connexion..." ou "Création..." mais rien ne se passe.
Dans la console VS Code :
```
Headers are read-only, response has already started.
System.InvalidOperationException
```

### Cause
C'est la **limitation fondamentale de Blazor Server** : on ne peut pas appeler
`SignInAsync()` ou `PasswordSignInAsync()` depuis un composant Blazor interactif.

Pourquoi ? Blazor Server communique via **SignalR** (WebSocket). La réponse HTTP
initiale est déjà envoyée au navigateur. Écrire un cookie d'authentification
nécessite de modifier les headers HTTP — impossible une fois la réponse démarrée.

### Solution
Remplacer les composants Blazor par de vrais **endpoints HTTP** dans `Program.cs`,
et utiliser des formulaires HTML natifs `<form method="post">` dans les pages :

```csharp
// Program.cs
app.MapPost("/account/login", async (
    SignInManager<AppUser> signInManager,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? returnUrl) =>
{
    var result = await signInManager.PasswordSignInAsync(email, password, false, false);
    if (result.Succeeded)
        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    return Results.Redirect("/login?error=1");
}).DisableAntiforgery();
```

```razor
@* Login.razor - formulaire HTML natif, pas de @code interactif *@
<form method="post" action="/account/login">
    <input type="email" name="email" />
    <input type="password" name="password" />
    <button type="submit">Se connecter</button>
</form>
```

---

## Problème 6 — `Required parameter "string email" was not provided from query string`

### Erreur
```
BadHttpRequestException: Required parameter "string email" was not provided from query string.
```

### Cause
Les endpoints Minimal API cherchaient les paramètres dans la **query string**
par défaut, alors qu'ils venaient du **corps du formulaire POST**.

### Solution
Ajouter l'attribut `[FromForm]` sur chaque paramètre :
```csharp
app.MapPost("/account/login", async (
    SignInManager<AppUser> signInManager,
    [FromForm] string email,      // ← [FromForm] obligatoire
    [FromForm] string password,
    [FromForm] string? returnUrl) => { ... });
```

---

## Problème 7 — Token antiforgery manquant

### Erreur
```
AntiforgeryValidationException: The required antiforgery request token was not provided
```

### Cause
Blazor active la protection **antiforgery** (anti-CSRF) par défaut sur tous les
endpoints POST. Nos formulaires HTML simples n'incluaient pas le token requis.

### Solution
Désactiver la vérification antiforgery sur les endpoints d'authentification
(acceptable car on gère notre propre sécurité via Identity) :
```csharp
app.MapPost("/account/login", async (...) => { ... })
   .DisableAntiforgery(); // ← ajouter ceci
```

---

## Problème 8 — `The value cannot be an empty string (Parameter 'url')`

### Erreur
```
ArgumentException: The value cannot be an empty string. (Parameter 'url')
```

### Cause
`returnUrl` valait `""` (chaîne vide) au lieu de `null` quand aucun returnUrl
n'était fourni. L'opérateur `??` ne couvre que `null`, pas `""`.

### Solution
Utiliser `string.IsNullOrEmpty()` à la place de `??` :
```csharp
// Avant (bugué)
return Results.Redirect(returnUrl ?? "/");

// Après (correct)
return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
```

---

## Résumé des leçons apprises

| Leçon | Règle |
|-------|-------|
| Blazor Server + cookies | Impossible d'écrire un cookie depuis un composant interactif — utiliser des endpoints HTTP |
| Guillemets Razor | Jamais de `"` dans un attribut `@onclick="..."` — extraire en méthode `@code` |
| Paramètres POST | Toujours `[FromForm]` pour lire les données d'un formulaire HTML |
| Antiforgery | `.DisableAntiforgery()` si on gère l'auth manuellement |
| String vide vs null | Préférer `string.IsNullOrEmpty()` à `??` pour les URLs |
| `_Imports.razor` | Fichier obligatoire en Blazor pour les `@using` globaux |