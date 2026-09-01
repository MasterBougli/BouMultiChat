# BouMultiChat

Application desktop Windows destinée aux créateurs qui diffusent simultanément sur plusieurs plateformes. Elle rassemblera les chats Twitch, YouTube, Kick, TikTok et les retours vidéo dans des colonnes indépendantes, avec un contrôle de délai de diffusion compatible avec un relais externe.

> Le développement de l’application n’a pas encore commencé. Le dépôt contient d’abord les règles d’architecture, de contribution et de sécurité qui encadreront le code.

## Objectifs

- afficher plusieurs chats et aperçus vidéo sur un seul écran ;
- sauvegarder une disposition différente selon le profil de diffusion ;
- isoler les pages web des plateformes du processus principal ;
- communiquer avec OBS par WebSocket sans exposer ses identifiants ;
- piloter un relais RTMP/SRT pour modifier le délai sans interrompre le live ;
- proposer une interface sombre, dense, accessible et utilisable au clavier.

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
4. Connexion OBS WebSocket sécurisée.
5. Contrat du contrôleur de délai externe.
6. Connecteurs Kick et TikTok selon leurs possibilités officielles.

## Construire le projet

Les commandes seront ajoutées avec le premier squelette applicatif. Le SDK .NET 9 et le runtime WebView2 seront requis.

## Contribuer

Consultez [CONTRIBUTING.md](CONTRIBUTING.md). Toute fonction ou méthode ajoutée au projet doit être commentée et documentée en français.

## Licence

BouMultiChat est distribué sous licence MIT. Voir [LICENSE](LICENSE).
