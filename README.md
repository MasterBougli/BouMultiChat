# BouMultiChat

Application desktop Windows destinée aux créateurs qui diffusent simultanément sur plusieurs plateformes. Elle rassemble les chats Twitch, YouTube, Kick et Trovo ainsi que les retours vidéo dans des colonnes indépendantes, avec un futur contrôle de délai compatible avec un relais externe.

Le développement a commencé par la frontière de sécurité : le dépôt contient une application WPF, une bibliothèque indépendante pour la validation des données non fiables et un projet de tests.

## Objectifs

- afficher plusieurs chats et aperçus vidéo sur un seul écran ;
- sauvegarder et restaurer automatiquement la disposition locale ;
- isoler les pages web des plateformes du processus principal ;
- communiquer avec OBS par WebSocket sans exposer ses identifiants ;
- piloter un relais RTMP/SRT pour modifier le délai sans interrompre le live ;
- proposer une interface sombre, dense, accessible et utilisable au clavier.

## État actuel

- ajout et suppression de quatre colonnes simultanées ;
- lecteur et chat Twitch à partir d’un nom de chaîne ;
- lecteur et chat YouTube à partir d’un identifiant vidéo ;
- lecteur et chat Kick à partir d’un nom de chaîne ;
- lecteur et chat Trovo à partir d’un nom de chaîne, sous réserve de l’autorisation de domaine demandée par Trovo ;
- chats verrouillés en lecture seule et zones d’écriture masquées ;
- sauvegarde et restauration automatiques dans les données locales Windows ;
- reconnexion automatique bornée et bouton de rechargement manuel ;
- icône dédiée dans la barre des tâches Windows ;
- isolation WebView2 par colonne ;
- blocage des domaines non autorisés, téléchargements, fenêtres secondaires, permissions sensibles et ponts JavaScript natifs ;
- validation automatisée des identifiants et des tentatives courantes d’injection.

OBS et le relais de délai restent à implémenter. TikTok LIVE ne fournit actuellement pas de chat officiel intégrable et n’est donc pas activé.

## Choix technique

- **C# et .NET 9** pour le code applicatif ;
- **WPF** pour l’interface Windows native ;
- **WebView2** pour les lecteurs et chats officiels ;
- **xUnit** pour les contrôles automatisés ;
- sérialisation JSON standard de .NET pour la configuration locale.

Ce choix privilégie une intégration Windows et OBS simple. Une version multiplateforme pourra être étudiée après validation du produit.

## Sécurité

Le contenu provenant des chats est considéré comme hostile. Il reste dans des WebView isolées et n’est jamais interprété comme du XAML, du HTML ou une commande système par BouMultiChat. Les règles complètes se trouvent dans [SECURITY.md](SECURITY.md) et [docs/MODELE_DE_MENACES.md](docs/MODELE_DE_MENACES.md).

## Feuille de route initiale

1. Fenêtre principale et gestion des colonnes.
2. Validation centralisée des plateformes et des URLs.
3. Intégration Twitch et YouTube.
4. Sauvegarde locale et connecteurs Kick et Trovo.
5. Connexion OBS WebSocket sécurisée.
6. Contrat du contrôleur de délai vers BouVideoServ.

## Construire le projet

Le SDK .NET 9 est requis.

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Le runtime WebView2 sera requis à partir de l’intégration des premières colonnes de chat.

## Contribuer

Consultez [CONTRIBUTING.md](CONTRIBUTING.md). Toute fonction ou méthode ajoutée au projet doit être commentée et documentée en français.

## Licence

BouMultiChat est distribué sous licence MIT. Voir [LICENSE](LICENSE).
