# Contribuer à BouMultiChat

## Langue et documentation

- Les commentaires, résumés XML, documents et messages destinés aux utilisateurs sont rédigés en français.
- Chaque fonction, méthode, constructeur et propriété publique possède une documentation XML en français.
- Chaque fonction privée possède un résumé expliquant son rôle, ses entrées non triviales et ses effets de bord.
- Un commentaire explique le **pourquoi** d’une décision ; il ne répète pas simplement le code.

## Qualité attendue

Avant une proposition de modification :

1. compiler sans avertissement ;
2. exécuter tous les tests ;
3. vérifier les règles de formatage ;
4. ajouter un test pour toute validation, branche de sécurité ou correction de défaut ;
5. mettre à jour la documentation liée au comportement modifié.

## Sécurité

- Considérez toute donnée externe comme hostile.
- N’ajoutez jamais de secret ou de jeton au dépôt.
- N’exposez jamais un objet .NET directement au JavaScript d’une WebView.
- Préférez une liste blanche explicite à une tentative de nettoyage d’une entrée arbitraire.
- Refusez une donnée invalide au lieu d’essayer de la corriger silencieusement.

## Commits et propositions

- Utilisez un titre court à l’impératif.
- Limitez chaque modification à un objectif cohérent.
- Décrivez le comportement, les tests et l’impact sécurité dans la proposition de modification.
