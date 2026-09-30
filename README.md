# Calculatrice Mobile — .NET MAUI

Activité n° 4 — Atelier de développement Mobile
Réalisé par : **DJOGU KAMENI Darelle**

Application de calculatrice mobile écrite en **C# avec .NET MAUI (XAML)**, utilisant
**7 types de layouts différents** dans une même page, une architecture **MVVM** et un
moteur de calcul **entièrement testé**.

---

## 1. Fonctionnalités

### Opérations de base (exigences de l'énoncé)

| Fonctionnalité | Touches |
|---|---|
| Addition, soustraction, multiplication, division | `+ − × ÷` |
| Nombres décimaux (virgule) | `,` |
| Remise à zéro totale / effacement de la saisie | `AC` / `CE` |
| Effacement du dernier caractère | `⌫` |
| Changement de signe | `±` |
| Pourcentage | `%` |
| Division par zéro gérée (aucun plantage) | `8 ÷ 0 =` → bannière d'erreur |
| Opération en cours affichée au-dessus du résultat | ligne `12 × 4` |

### Opérations avancées (points bonus)

- Puissance `xʸ` (par exemple `2 xʸ 10 =` → `1024`)
- Racine carrée `√` (erreur gérée sur une valeur négative)
- Carré `x²`
- Inverse `1/x`
- Pourcentage relatif (`200 + 10 % =` → `220`)
- **4 cases mémoire** : `MC`, `MR`, `M+`, `M−` avec badge `M` dans l'en-tête
- **Historique** des 25 derniers calculs, consultable et réutilisable (toucher un
  élément recharge sa valeur)
- **Répétition** du dernier calcul avec `=` (`2 + 3 = =` → `8`)
- Thème sombre, icône et écran de démarrage personnalisés

### Robustesse (cas limites traités)

- Saisie limitée à 12 chiffres significatifs (message d'information)
- Zéros initiaux supprimés (`005` → `5`), une seule virgule par nombre
- Opérateur saisi deux fois de suite → le second remplace le premier
- `√` d'un nombre négatif, `1/x` sur 0, `0^(-1)`, exposant fractionnaire sur base
  négative : message d'erreur explicite, l'application reste utilisable
- Débordement de capacité (`999999999 × 999999999`) : refusée au lieu d'afficher
  `∞` ou une notation illisible
- Formatage intelligent : virgule décimale, pas de zéros superflus, notation
  scientifique pour les très petits nombres, `0` pour `-0`
- Taille de police du résultat adaptée à sa longueur (46 → 22)

---

## 2. Layouts utilisés (7 conteneurs, dans une seule page)

| Layout | Où | Justification |
|---|---|---|
| **Grid** | `MainPage.xaml` : racine + pavé numérique 4 × 5 | Répartit précisément l'espace en lignes/colonnes et permet d'étirer `=` sur deux colonnes. |
| **VerticalStackLayout** | Affichage, barre de titre, historique | Empile les blocs de haut en bas sans calcul de hauteur. |
| **HorizontalStackLayout** | En-tête, ligne mémoire, ligne d'un élément d'historique | Aligne sur une rangée des éléments de même hauteur. |
| **ScrollView** | Panneau d'affichage + pavé numérique + historique | Évite tout débordement sur petit écran ou en paysage (exigence de l'énoncé). |
| **FlexLayout** | Rangée des touches scientifiques | `Wrap="Wrap"` : les touches passent à la ligne automatiquement si la largeur manque. |
| **Border** | Carte d'affichage, capsules mémoire, pastille d'historique | Coins arrondis et couleur de fond sans codes Cull et sans `Frame` (obsolète en .NET 9). |
| **AbsoluteLayout** | Bannière d'erreur superposée en haut | Affiche le message d'erreur **par-dessus** l'interface sans perturber la disposition. |

Le détail et les justifications figurent aussi dans le rapport PDF d'une page
fourni avec l'activité.

---

## 3. Architecture du projet

```
Activite4_Calculatrice_MAUI_DJOGU_KAMENI_Darelle/
├── CalculatriceMaui/                  # projet .NET MAUI (Single Project)
│   ├── App.xaml(.cs)                  # thème sombre imposé, fenêtre
│   ├── AppShell.xaml(.cs)             # Shell, barre de navigation masquée
│   ├── MainPage.xaml                  # interface complète (les 7 layouts)
│   ├── MainPage.xaml.cs               # code-behind : événements + orientation
│   ├── MauiProgram.cs                 # démarrage de l'application
│   ├── Models/
│   │   └── CalculatorEngine.cs        # moteur de calcul (sans dépendance MAUI)
│   ├── ViewModels/
│   │   └── CalculatorViewModel.cs     # MVVM : INotifyPropertyChanged, liaison
│   ├── Resources/                     # Colors.xaml, Styles.xaml, icônes, polices
│   └── Platforms/                     # Android, iOS, MacCatalyst, Windows
├── Tests/EngineTests/                 # 79 tests automatiques du moteur
├── build-apk.sh / build-apk.ps1       # scripts de compilation et d'installation
├── Rapport_Activite4_...pdf           # rapport d'une page (layouts + justifications)
└── Guide_video_15s.md                 # scénario de la vidéo de démonstration
```

**Séparation des responsabilités**

- `CalculatorEngine` ne contient **aucune dépendance à l'interface** : il reçoit des
  touches et renvoie un résultat (`CalculatorAction`) avec un éventuel message
  d'erreur. Il est donc testable sur n'importe quelle machine.
- `CalculatorViewModel` implémente `INotifyPropertyChanged` : c'est lui qui est lié
  au XAML (`Display`, `Expression`, `HasError`, `History`, `HasMemory`…).
- `MainPage.xaml.cs` ne fait que lier les `Clicked` du XAML aux méthodes du
  ViewModel, et réorganise la `Grid` racine au changement d'orientation.

---

## 4. Compiler, installer et lancer

> La procédure détaillée, étape par étape (installation de la charge de travail
> MAUI, du JDK et du SDK Android, compilation, installation sur le téléphone) se
> trouve dans **`PROCEDURE_INSTALLATION.md`**.

Prérequis : le SDK .NET et la charge de travail MAUI.

```bash
dotnet workload install maui-android
dotnet build CalculatriceMaui/CalculatriceMaui.csproj -f net10.0-android
```

Installer l'APK sur un téléphone ou un émulateur Android :

```bash
./build-apk.sh          # Linux / macOS
.\build-apk.ps1         # Windows
```

Lancer en mode débogage directement depuis la ligne de commande :

```bash
dotnet build CalculatriceMaui/CalculatriceMaui.csproj -t:Run -f net10.0-android
```

> Sous Linux, le SDK .NET MAUI ne peut produire que la cible **Android** ; les cibles
> iOS et MacCatalyst exigent un Mac avec Xcode. Le projet multi-cible reste déclaré
> dans le `.csproj` (`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`).

### Vérifier la logique sans téléphone

Le moteur de calcul est compilé et testé sur n'importe quel ordinateur (aucun
émulateur requis) :

```bash
dotnet run --project Tests/EngineTests
# → Tests réussis : 79 / Tous les tests passent.
```

---

## 5. Versionnement et dépôt Git

```bash
git init
git add .
git commit -m "Calculatrice .NET MAUI - Activite 4"
git remote add origin https://github.com/<utilisateur>/<depot>.git
git push -u origin main
```
