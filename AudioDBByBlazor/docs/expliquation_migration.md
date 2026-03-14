# 📝 Notes personnelles – Entity Framework Core & Base de données

## C'est quoi Entity Framework Core ?

EF Core, c'est un outil qui fait le lien entre mon code C# et une base de données.
L'idée, c'est que **je n'écris jamais de SQL moi-même** — j'écris des classes C#,
et EF Core se charge de créer les tables correspondantes. On appelle ça le **Code First**.

---

## Ce qu'on a fait concrètement

### 1. J'ai installé l'outil EF en ligne de commande

```bash
dotnet tool install --global dotnet-ef
```

C'est comme installer une application sur mon PC. Ça me donne accès à la commande
`dotnet ef` dans le terminal, qui me permet de gérer ma base de données.

---

### 2. J'ai créé un fichier de migration

```bash
dotnet ef migrations add InitialCreate
```

EF Core a **lu mes classes C#** (surtout `AppUser.cs` et `AppDbContext.cs`) et a
généré automatiquement un fichier C# dans un dossier `Migrations/`.

Ce fichier contient les instructions pour créer les tables dont j'ai besoin,
notamment toutes les tables liées à l'authentification :
- `AspNetUsers` → les comptes utilisateurs
- `AspNetRoles` → les rôles (admin, user...)
- `AspNetUserRoles` → qui a quel rôle
- etc.

> 💡 Je n'ai pas eu à écrire ce fichier à la main — EF l'a déduit tout seul
> depuis mes classes.

---

### 3. J'ai appliqué la migration sur la base de données

```bash
dotnet ef database update
```

Cette commande a **exécuté concrètement** les instructions du fichier de migration.
Résultat : un fichier `audiodb.db` a été créé à la racine de mon projet.
C'est ma base de données SQLite — un simple fichier qui contient toutes mes tables.

---

## Le flux complet résumé

```
Mes classes C#          Migration (fichier)        Base de données
(AppUser, etc.)   →→→   (Migrations/*.cs)    →→→   (audiodb.db)

dotnet ef               dotnet ef                  dotnet ef
(installé)              migrations add             database update
                        InitialCreate
```

---

## Pourquoi SQLite ?

SQLite, c'est une base de données qui tient dans **un seul fichier** sur mon disque.
Pas besoin d'installer un serveur (comme MySQL ou SQL Server).
C'est parfait pour un projet de cours ou une démo — simple, léger, portable.

---

## Et si je modifie mes classes C# plus tard ?

Si j'ajoute une propriété à `AppUser` par exemple, je recrée une migration :

```bash
dotnet ef migrations add AjoutPropriete
dotnet ef database update
```

EF comparera l'état actuel de mes classes avec la dernière migration,
et générera uniquement les **changements nécessaires**.