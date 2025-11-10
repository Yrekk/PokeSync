PokéSync – Backend .NET 10 + EF Core + SQL Server

🎮 PokéSync est une plateforme d’intégration de données Pokémon, développée avec .NET 10 (RC2), Entity Framework Core, et SQL Server LocalDB.Ce projet sert de vitrine technique (architecture propre, CI/CD, intégration API, documentation automatique).

 Architecture

 PokeSync/
 ├── src/
 │   ├── PokeSync.Api/               → API REST (.NET 10)
 │   ├── PokeSync.Domain/            → Entités et logique métier
 │   ├── PokeSync.Infrastructure/    → Accès aux données (EF Core, SQL Server)
 │   ├── PokeSync.Shared/            → Objets transverses / DTO / Helpers
 │   └── PokeSync.Tests/             → Tests unitaires et d’intégration
 ├── docs/                           → Documentation technique
 ├── .github/                        → CI/CD, templates PR/Issues
 ├── .gitignore
 ├── PokeSync.slnx
 └── README.md

 Installation & exécution locale

1️⃣ Prérequis

.NET 10 SDK RC2 (10.0.100-rc.2.25502.107)

SQL Server LocalDB ou Docker SQL Server

dotnet-ef installé globalement :
dotnet tool install --global dotnet-ef

2️⃣ Initialiser la base de données

Depuis la racine du projet (PokeSync/) :
dotnet ef migrations add InitialCreate -p src/PokeSync.Infrastructure -s src/PokeSync.Api
dotnet ef database update -p src/PokeSync.Infrastructure -s src/PokeSync.Api

🔹 Les migrations EF Core sont exécutées automatiquement au démarrage si le flag AutoMigrate = true dans appsettings.Development.json.

3️⃣ Lancer l’API
cd src/PokeSync.Api
dotnet run

Par défaut :

🌐 API → https://localhost:7200

📜 OpenAPI Spec → /openapi/v1.json

💻 Scalar UI (Swagger Next-Gen) → /scalar

🧠 Stack technique

Backend API : .NET 10 RC2 (ASP.NET Core Minimal API)

ORM : Entity Framework Core 9

Database : SQL Server LocalDB / Docker SQL

Logging : Serilog

Validation : FluentValidation

Documentation API : OpenAPI / Scalar

Tests : xUnit + EFCore.InMemory

 Tests unitaires & d’intégration

 cd src/PokeSync.Tests
dotnet test


🧰 Commandes utiles
🔧 Créer une migration :  dotnet ef migrations add NomMigration -p src/PokeSync.Infrastructure -s src/PokeSync.Api
🗃️ Appliquer les migrations : dotnet ef database update -p src/PokeSync.Infrastructure -s src/PokeSync.Api

🧑‍💻 Auteur

Damien (Yrekk)💼 Développeur .NET / Salesforce / MuleSoft🌐 github.com/Yrekk

🧱 État du projet

✅ MVP0 : Socle backend (.NET + SQL) avec migration automatique
🚧 MVP1 : Repositories + Controllers REST (en cours)
🧩 MVP2 : Intégration MuleSoft
🌍 MVP3 : Interface Angular pour PokéSync


🛠️ CI/CD (prochainement)
Un workflow GitHub Actions (.github/workflows/dotnet.yml) sera ajouté pour automatiser le build, les tests et les migrations EF sur chaque PR.
📜 Licence

Projet open-source à visée démonstrative – © 2025 Damien (Yrekk)

