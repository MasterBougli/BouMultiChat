# Modèle de menaces

## Actifs à protéger

- identifiants OAuth des plateformes ;
- mot de passe OBS WebSocket ;
- configuration et profils de diffusion ;
- poste Windows de l’utilisateur ;
- intégrité de la diffusion en cours.

## Frontières de confiance

Les pages Twitch, YouTube, Kick et TikTok, les messages de chat, OBS, le relais de délai et tout fichier de configuration importé se trouvent hors de la frontière de confiance du processus principal.

## Menaces principales et protections

### Injection par un message de chat

Un message peut contenir du HTML, du JavaScript, du XAML, une URL ou une commande. BouMultiChat ne récupère pas le DOM d’un chat pour le réafficher dans WPF. Le contenu reste dans la WebView de la plateforme, sans pont vers .NET.

### Navigation vers un site hostile

Une page intégrée peut tenter une redirection ou une ouverture de fenêtre. Chaque navigation est vérifiée sur le schéma, le nom d’hôte exact, le port et le contexte de la plateforme. Les fenêtres secondaires sont refusées.

### Vol de secrets

Les secrets ne sont ni placés dans les URLs ni écrits dans les journaux ou le JSON de configuration. Ils sont chiffrés pour le compte Windows courant et ne sont déchiffrés qu’au moment de la connexion concernée.

### Commande non autorisée vers OBS ou le relais

Les commandes disponibles sont codées explicitement. Aucun nom de méthode, corps JSON ou chemin n’est construit directement à partir d’un message de chat. Les valeurs numériques sont bornées et les réponses distantes sont validées.

### Déni de service

Le nombre de colonnes, la longueur des entrées, la taille des réponses et les délais réseau seront limités. Une WebView défaillante pourra être arrêtée indépendamment sans fermer l’application.

## Risques résiduels

Une plateforme peut modifier ses règles d’intégration, afficher du contenu trompeur dans sa propre WebView ou bloquer l’embed. Les connecteurs non officiels resteront désactivés par défaut tant qu’une méthode fiable n’est pas validée.
