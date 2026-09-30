# Pas à pas — Linux (Ubuntu) + VS Code

Machine vérifiée : Ubuntu, `dotnet` 10.0.401 déjà installé dans `~/.dotnet`,
`adb` déjà présent (`/usr/bin/adb`), `/dev/kvm` disponible.
Il reste donc à installer : **le JDK**, **la charge de travail MAUI**, **le SDK Android**.

Durée totale : ~20 à 30 minutes, dont l'essentiel en téléchargements.
Chaque étape est indépendante : colle le bloc de commandes dans le **terminal intégré de VS Code**
(`Ctrl+` + `` ` ``) et attends la fin avant de passer à la suivante.

---

## Étape 0 — Vérifier la base

```bash
dotnet --version
df -h / | tail -1
```

Tu dois voir `10.0.4xx` et au moins **5 Go libres**. Si `dotnet` n'est pas reconnu,
ajoute ceci puis ferme/réouvre le terminal :

```bash
echo 'export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

---

## Étape 1 — Installer le Java (JDK 21)

Sans Java, la compilation Android échoue.

```bash
sudo apt update
sudo apt install -y openjdk-21-jdk wget unzip

java -version
```

Doit afficher `21.x`. Ensuite, note le chemin exact de Java :

```bash
readlink -f "$(which java)" | sed 's|/bin/java||'
```

Copie le chemin affiché (ex. `/usr/lib/jvm/java-21-openjdk-amd64`) dans `.bashrc` :

```bash
echo 'export JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64' >> ~/.bashrc
echo 'export PATH=$PATH:$JAVA_HOME/bin' >> ~/.bashrc
source ~/.bashrc
```

> Adapte le chemin si le tien diffère (OpenJDK 17 fonctionne aussi :
> `sudo apt install -y openjdk-17-jdk`).

---

## Étape 2 — Installer la charge de travail .NET MAUI

```bash
cd ~/Téléchargements/Activite4_Calculatrice_MAUI_DJOGU_KAMENI_Darelle
dotnet workload install maui-android
```

C'est le plus long téléchargement (~1 Go). Contrôle :

```bash
dotnet workload list | grep maui-android
```

Doit afficher `maui-android`.

---

## Étape 3 — Installer le SDK Android

```bash
mkdir -p ~/Android/sdk/cmdline-tools
cd ~/Android/sdk/cmdline-tools

wget https://dl.google.com/android/repository/commandlinetools-linux-13114758_latest.zip
unzip commandlinetools-linux-*.zip
mv cmdline-tools latest
```

Rendre les variables permanentes :

```bash
cat >> ~/.bashrc <<'EOF'

export ANDROID_HOME=$HOME/Android/sdk
export PATH=$PATH:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools
EOF
source ~/.bashrc
```

Accepter les licences et installer les composants :

```bash
yes | sdkmanager --licenses

sdkmanager "platform-tools" "platforms;android-36" "build-tools;36.0.0"
```

Contrôle :

```bash
sdkmanager --list_installed
```

Dois voir `platform-tools`, `platforms;android-36`, `build-tools;36.0.0`.
> .NET 10 cible l'API Android 36 : si la compilation affiche `XA5207` (android.jar introuvable),
> c'est que `platforms;android-36` n'est pas installé.

---

> **Si l'installation de la charge de travail échoue avec « Disk quota exceeded » ou
> « Downloading ... failed »** : le dossier `/tmp` est souvent un `tmpfs` limité en taille.
> Utilise un dossier temporaire sur le disque :
>
> ```bash
> mkdir -p ~/tmp-build
> dotnet workload install maui-android --temp-dir ~/tmp-build
> ```

---

## Étape 4 — Vérifier la logique du moteur (aucun téléphone requis)

```bash
cd ~/Téléchargements/Activite4_Calculatrice_MAUI_DJOGU_KAMENI_Darelle
dotnet run --project Tests/EngineTests
```

Attendu :

```
Tests réussis : 79
Tous les tests passent.
```

---

## Étape 5 — Compiler l'APK

En **Debug**, l'APK est signé automatiquement et directement installable
(le mode Release demande de créer une clé de signature, inutile pour une démo).

```bash
dotnet build CalculatriceMaui/CalculatriceMaui.csproj -f net10.0-android -c Debug
```

L'APK sort ici :

```
CalculatriceMaui/bin/Debug/net10.0-android/com.djogukameni.calculatrice-Signed.apk
```

Vérification :

```bash
ls -lh CalculatriceMaui/bin/Debug/net10.0-android/*.apk
```

---

## Étape 6 — Installer sur ton téléphone

1. Téléphone : **Paramètres → À propos** → appuyer 7 fois sur « Numéro de build »
   → mode développeur activé.
2. Téléphone : **Paramètres → Options pour les développeurs → Débogage USB** → activé.
3. Branche le téléphone en USB, **accepte la boîte « Autoriser le débogage USB »** sur le téléphone.

Puis, sur l'ordinateur :

```bash
adb devices
```

Le téléphone doit apparaître avec la mention `device` (pas `unauthorized`).

```bash
adb install -r CalculatriceMaui/bin/Debug/net10.0-android/com.djogukameni.calculatrice-Signed.apk
adb shell monkey -p com.djogukameni.calculatrice -c android.intent.category.LAUNCHER 1
```

L'application s'ouvre sur le téléphone.

Raccourci (compile + installe + lance d'un coup) :

```bash
./build-apk.sh Debug
```

---

## Étape 7 — Lancer et déboguer sans réinstaller à la main

```bash
dotnet build CalculatriceMaui/CalculatriceMaui.csproj -t:Run -f net10.0-android
```

Le code est compilé, installé et lancé ; les logs s'affichent dans le terminal.

---

## Étape 8 — (Optionnel) Utiliser un émulateur au lieu du téléphone

`/dev/kvm` est disponible sur cette machine, donc l'accélération matérielle fonctionne.
Compte ~2 Go de téléchargement supplémentaire.

```bash
sdkmanager "system-images;android-35;google_apis;x86_64" "emulator"
echo "no" | avdmanager create avd -n test_calculatrice -k "system-images;android-35;google_apis;x86_64" --device "pixel_6"
$ANDROID_HOME/emulator/emulator -avd test_calculatrice -no-snapshot &
adb wait-for-device
```

Une fois l'émulateur démarré :

```bash
dotnet build CalculatriceMaui/CalculatriceMaui.csproj -t:Run -f net10.0-android
```

> Sur cette machine (7 Go de RAM), l'émulateur sera lent : le téléphone réel reste
> plus confortable pour la vidéo.

---

## Étape 9 — VS Code

- `Fichier → Ouvrir le dossier…` → `Activite4_Calculatrice_MAUI_DJOGU_KAMENI_Darelle`
- Extensions utiles : **C# Dev Kit** (gratuit, ou l'extension **C#** de Microsoft),
  et **MAUI Community Toolkit** si tu veux des composants supplémentaires.
- Les commandes des étapes 4 à 7 se lancent dans le terminal intégré : c'est plus
  fiable que le bouton ▶ de VS Code pour une cible Android unique.
- Si l'extension C# affiche « .NET SDK introuvable », ajouter dans
  `~/.config/Code/User/settings.json` :

```json
"dotnet.sdkPath": "/home/darelle/.dotnet/dotnet"
```

---

## Étape 10 — Déposer le dépôt GitHub

```bash
cd ~/Téléchargements/Activite4_Calculatrice_MAUI_DJOGU_KAMENI_Darelle
git remote add origin https://github.com/<ton-compte>/<ton-depot>.git
git branch -M main
git push -u origin main
```

Créer d'abord le dépôt **vide** sur github.com (sans README), sinon le push est refusé.

---

## Récapitulatif des problèmes courants

| Message | Solution |
|---|---|
| `Could not find android.jar` | `ANDROID_HOME` non défini → refaire l'étape 3 (`source ~/.bashrc`, nouvelle fenêtre de terminal). |
| `JAVA_HOME not set` | Reprendre la deuxième partie de l'étape 1. |
| `workload ... maui-android` manquant | Refaire `dotnet workload install maui-android`. |
| `error NETSDK1147: pour/water` | Oubli de `-f net10.0-android` dans la commande de build. |
| `adb: device unauthorized` | Sur le téléphone : « Paramètres du développeur → Révoquer les autorisations USB », puis rebrancher. |
| Build très lent (> 5 min) | Normal la première fois (téléchargement des packs). Les suivants prennent ~20 s. |
| Erreur de signature de l'APK en Release | Utiliser `-c Debug` (étape 5). |

---

## Checklist avant le dépôt

- [ ] `dotnet run --project Tests/EngineTests` → 79 tests réussis
- [ ] APK compilé et lancé sur le téléphone
- [ ] Rotation de l'appareil testée (affichage à gauche, clavier à droite)
- [ ] `8 ÷ 0 =` affiche une erreur sans planter
- [ ] Vidéo de 15 s enregistrée (scénario dans `Guide_video_15s.md`)
- [ ] `git push` effectué sur un dépôt **public**
