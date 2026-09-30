# Tests du moteur de calcul

Ces tests compilent les fichiers `Models/CalculatorEngine.cs` et
`ViewModels/CalculatorViewModel.cs` de l'application **sans dépendre de .NET MAUI** :
ils s'exécutent donc sur n'importe quel ordinateur, en quelques secondes, sans
émulateur ni téléphone.

```bash
dotnet run --project Tests/EngineTests
```

Résultat attendu :

```
Tests réussis : 79
Tous les tests passent.
```

Les 79 vérifications couvrent : les quatre opérations de base, la saisie décimale,
la correction (`⌫`, `CE`, `AC`), le changement de signe, le pourcentage (absolu et
relatif), la division par zéro et la reprise après erreur, les opérations avancées
(√, x², 1/x, xʸ), les cas limites (racine négative, 0 puissance −1, exposant
fractionnaire, dépassement de capacité, saisie limitée à 12 chiffres), le formatage
des nombres, les quatre cases mémoire, l'historique (réutilisation, vidage, limite
de 25 éléments) et l'affichage de l'opération en cours.
