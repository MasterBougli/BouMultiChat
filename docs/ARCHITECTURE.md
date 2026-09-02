# Architecture cible

## Vue générale

BouMultiChat suivra une architecture simple en quatre zones :

1. **Interface WPF** : fenêtres, commandes et disposition des colonnes.
2. **Connecteurs de plateformes** : construction d’URLs autorisées et capacités déclarées par plateforme.
3. **Services locaux** : persistance, secrets Windows et connexion OBS WebSocket.
4. **Contrôleur de délai** : contrat réseau vers un relais RTMP/SRT externe.

L’interface dépendra des services applicatifs. Les connecteurs ne pourront pas appeler l’interface, le système de fichiers ou lancer un processus.

## Isolation des contenus web

Chaque colonne utilisera des environnements WebView2 configurés sans pont JavaScript natif. Les événements de navigation seront contrôlés avant chargement. Une page ne pourra pas :

- naviguer vers un domaine non autorisé ;
- ouvrir une fenêtre secondaire ;
- démarrer un téléchargement ;
- appeler une commande native ;
- lire les secrets d’une autre colonne.

## Données locales

La disposition et les identifiants publics de chaînes sont enregistrés atomiquement en JSON dans les données locales Windows. Chaque entrée rechargée repasse par la validation de liste blanche. Les futurs mots de passe et jetons seront conservés séparément, chiffrés pour l’utilisateur Windows courant.

## Délai de diffusion

Le délai OBS natif n’est pas modifiable pour une sortie déjà active. L’application pilotera donc BouVideoServ, qui reçoit déjà le RTMP avec MediaMTX. Un processus FFmpeg devra relire l’entrée, maintenir un tampon audio/vidéo puis publier une sortie distincte vers chaque plateforme. Son API restera locale, authentifiée et bornée à une plage de délai configurable.

## Contrôle de BouVideoServ

BouMultiChat ne charge pas le serveur dans son propre processus. Il conserve le chemin absolu explicitement approuvé, lance BouVideoServ avec son dossier de travail, puis vérifie séparément :

- la route locale `/api/health` ;
- le champ `running` de `/api/engine` ;
- l’ouverture TCP du port RTMP `1935`.

L’arrêt et le redémarrage sont refusés si le chemin du processus actif ne correspond pas exactement au binaire sélectionné. La fermeture de BouMultiChat ne coupe pas automatiquement le serveur.
