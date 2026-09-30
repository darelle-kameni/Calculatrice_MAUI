#!/usr/bin/env bash
#
# Compile l'application .NET MAUI pour Android, puis installe l'APK
# sur l'appareil Android connected ou sur le premier émulateur disponible.
#
# Usage :  ./build-apk.sh [Release|Debug]
#
set -euo pipefail

CONFIGURATION="${1:-Release}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$PROJECT_DIR/CalculatriceMaui/CalculatriceMaui.csproj"
TARGET_FRAMEWORK="net10.0-android"

echo "==> Configuration : $CONFIGURATION"

if ! dotnet workload list | grep -q "maui-android"; then
    echo "==> Installation de la charge de travail maui-android..."
    dotnet workload install maui-android
fi

echo "==> Nettoyage et compilation..."
dotnet clean "$PROJECT" -f "$TARGET_FRAMEWORK" -c "$CONFIGURATION"
dotnet build "$PROJECT" -f "$TARGET_FRAMEWORK" -c "$CONFIGURATION" -p:AndroidPackageFormat=apk

APK_PATH="$PROJECT_DIR/CalculatriceMaui/bin/$TARGET_FRAMEWORK/$CONFIGURATION/com.djogukameni.calculatrice-Signed.apk"
[ -f "$APK_PATH" ] || APK_PATH="$(find "$PROJECT_DIR/CalculatriceMaui/bin/$TARGET_FRAMEWORK/$CONFIGURATION" -name '*.apk' | head -n 1)"

echo "==> APK généré : $APK_PATH"

if ! command -v adb >/dev/null 2>&1; then
    echo "==> 'adb' est introuvable : copiez manuellement l'APK sur votre téléphone."
    exit 0
fi

DEVICE="$(adb devices | awk 'NR>1 && $2=="device" {print $1; exit}')"

if [ -z "$DEVICE" ]; then
    echo "==> Aucun appareil détecté. Démarrez un émulateur puis relancez le script."
    exit 0
fi

echo "==> Installation sur l'appareil $DEVICE..."
adb -s "$DEVICE" install -r "$APK_PATH"
adb -s "$DEVICE" shell monkey -p com.djogukameni.calculatrice -c android.intent.category.LAUNCHER 1 >/dev/null

echo "==> Terminé : la calculatrice est lancée."
