# Interface Blazor — Congé

Cette interface est une Blazor Web App .NET 10 avec des composants Razor et le mode **Interactive Server**. Elle appelle l’API HTTP ; elle n’accède pas directement à SQL Server ou à Identity.

## Lancer l’interface

Configurer et démarrer l’API avec les instructions du [README principal](../README.md), puis :

```powershell
dotnet run --project HR.LeaveManagement.BlazorUI --launch-profile https
```

Ouvrir `https://localhost:7040/connexion`. La même page connecte les administrateurs et les employés ; les rôles attribués dans Identity déterminent ensuite les écrans et actions accessibles.

L’adresse de l’API se configure avec `Api:BaseUrl` dans les paramètres du projet ou `Api__BaseUrl` dans l’environnement. Avec les profils HTTP :

```powershell
$env:Api__BaseUrl = "http://localhost:5225/"
dotnet run --project HR.LeaveManagement.BlazorUI --launch-profile http
```

Aucun compte administrateur par défaut n’est créé. Pour le premier accès en développement, inscrire un compte employé sur `/inscription`, puis suivre la procédure du README principal pour lui attribuer le rôle administrateur et se reconnecter.

## Pages et rôles

| Route | Contenu | Administrateur | Employé |
|---|---|---|---|
| `/connexion` | Connexion, affichage du mot de passe, erreurs de saisie | Oui | Oui |
| `/` | Tableau de bord et dernières demandes | Équipe | Ses données |
| `/utilisateurs` | Ajouter, chercher, lister, modifier et supprimer des comptes | Oui | Non |
| `/types-conge` | Ajouter, chercher, lister, modifier et supprimer des types | Oui | Non |
| `/allocations` | Filtrer les soldes par année et rechercher une personne ou un type | Gestion | Lecture personnelle |
| `/demandes` | Saisir, modifier, suivre et annuler les demandes | Toutes + décisions et suppression | Ses demandes |

L’attribution des allocations crée les quotas **manquants** pour tous les employés, pour un type et l’année UTC courante. Les quotas existants ne sont pas réinitialisés. La modification permet d’ajuster le solde d’une allocation de l’année courante ou future ; le type et l’année restent inchangés.

Une demande approuvée doit être annulée avant suppression. Une allocation liée à une demande approuvée active ne peut pas être supprimée. Un type référencé ou un compte ayant des données métier ne peut pas être supprimé : le message API indique le motif du refus.

## Organisation du code

```text
Components/
├── App.razor                   Document HTML, ressources et Interactive Server
├── Routes.razor                Router et AuthorizeRouteView
├── Layout/
│   ├── MainLayout.razor        Navigation, en-tête, compte et déconnexion
│   ├── AuthLayout.razor        Présentation des pages publiques
│   └── NavMenu.razor           Liens adaptés au rôle
├── Pages/
│   ├── Login.razor             Connexion
│   ├── Register.razor          Inscription locale
│   ├── Home.razor              Tableau de bord
│   ├── Requests.razor          Demandes de congé
│   └── Administration/        Utilisateurs, types et allocations
└── Shared/
    ├── ApiPageBase.cs          Chargement, erreurs et messages de succès
    ├── ConfirmDialog.razor    Confirmation native des actions définitives
    ├── Feedback.razor         Messages accessibles et état de chargement
    ├── PageHeader.razor       Titre et actions d’une page
    ├── EmptyState.razor       État vide d’une liste
    └── AppIcon.razor          Icônes SVG locales
Models/ApiModels.cs            Modèles de formulaire et données reçues
Services/LeaveApiClient.cs     Appels HTTP et session du circuit
Services/ApiAuthenticationStateProvider.cs
wwwroot/app.css               Thème et adaptation aux petits écrans
wwwroot/app.js                Ouverture des dialogues natifs
```

## Formulaires et validation

Les pages utilisent `EditForm` avec un modèle, `OnValidSubmit`, `DataAnnotationsValidator` et des messages par champ. Les annotations dans `Models/ApiModels.cs` contrôlent les champs obligatoires, l’e-mail, la longueur et les quotas. `IValidatableObject` permet de comparer les deux dates ou les deux mots de passe.

Un modèle de formulaire est une copie des données de la ligne : annuler une modification ne change pas la liste. `@key` réinitialise correctement le contexte du formulaire lorsqu’un autre élément est sélectionné. Après une opération réussie, les données sont relues depuis l’API.

`ApiPageBase.RunAsync` désactive les commandes pendant un appel, intercepte les erreurs réseau et affiche le succès ou le message d’erreur. Il ne remplace pas les règles métier de l’API.

## État, session et autorisations

`LeaveApiClient` est enregistré avec `AddScoped` : son jeton appartient à un seul circuit utilisateur. La connexion obtient le JWT puis les informations de `/api/auth/me`. L’événement `SessionChanged` notifie `ApiAuthenticationStateProvider`, qui met à jour l’identité et les rôles vus par Blazor.

Les pages administratives portent `[Authorize(Roles = "Administrator")]`. `AuthorizeRouteView` redirige l’utilisateur non connecté vers la connexion et affiche un refus pour un rôle insuffisant. Les routes API appliquent aussi leurs propres autorisations.

Une déconnexion, un jeton expiré, une suppression de compte ou un changement de rôle provoquant un refus HTTP 401 efface la session du client. Le jeton reste en mémoire : un rechargement complet de la page demande une nouvelle connexion.

## Vérifications

```powershell
dotnet build HR.LeaveManagement.Clean.slnx
dotnet test --solution HR.LeaveManagement.Clean.slnx
```

Les tests couvrent notamment l’isolation des sessions, la mise à jour de l’état Blazor, le CRUD des comptes, les droits administrateur et l’invalidation des anciens jetons. Les tests SQL utilisent une base temporaire distincte, décrite dans le README principal.
