# Politique de sécurité

## Signaler une vulnérabilité

Ne publiez pas de vulnérabilité exploitable dans une issue publique. Utilisez la fonction **Security Advisories** du dépôt GitHub en fournissant :

- la zone concernée ;
- les étapes minimales de reproduction ;
- l’impact possible ;
- une proposition de correction si elle est connue.

Aucun secret, jeton OAuth, mot de passe OBS ou contenu privé de chat ne doit être joint au rapport.

## Principes obligatoires

- Toute donnée reçue d’une plateforme, d’un chat, d’OBS ou d’un relais est non fiable.
- Les domaines autorisés sont définis explicitement ; une URL arbitraire n’est jamais chargée par défaut.
- Seul HTTPS est accepté pour les services distants. Les exceptions locales sont limitées aux adresses de boucle locale.
- Les redirections, fenêtres secondaires, téléchargements et ouvertures de protocoles externes sont bloqués par défaut.
- Aucun objet natif n’est exposé au JavaScript des WebView.
- Aucun message de chat n’est transformé en XAML, HTML applicatif, chemin de fichier, commande ou requête réseau.
- Les identifiants OBS sont protégés avec les mécanismes de chiffrement liés au compte Windows.
- Les journaux excluent les secrets, jetons, URLs signées et textes privés des utilisateurs.
- Les entrées sont validées par liste blanche, limitées en longueur et refusées en cas d’ambiguïté.
- Les dépendances sont réduites, verrouillées et contrôlées avant chaque publication.

## Périmètre des tests de sécurité

Chaque connecteur doit tester au minimum :

- le refus des schémas autres que HTTPS ;
- le refus des sous-domaines trompeurs et des noms d’hôte ressemblants ;
- le refus des URLs contenant des identifiants intégrés ;
- le blocage des navigations hors liste blanche ;
- la neutralité des charges HTML, JavaScript, XAML et commandes système dans les champs texte ;
- les limites de taille et les valeurs nulles.
