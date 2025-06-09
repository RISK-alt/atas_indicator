# Multi-Indicateur ATAS

## Description
Cet indicateur pour ATAS combine plusieurs fonctionnalités essentielles pour l'analyse technique :
- Niveaux de la journée précédente
- Zones des 30 premières minutes
- Profil de volume avec POC et zones de faible volume

## Installation
1. Copiez le fichier `ATAS_MultiIndicator_20250609_125939.cs` dans le dossier des indicateurs ATAS :
   - Ouvrez ATAS
   - Allez dans `Fichier > Ouvrir le dossier des indicateurs`
   - Collez le fichier dans ce dossier
2. Redémarrez ATAS
3. L'indicateur apparaîtra dans la liste des indicateurs disponibles sous le nom "Multi-Indicateur ATAS"

## Fonctionnalités

### 1. Niveaux de la journée précédente
- **Points calculés automatiquement** :
  - Point le plus haut
  - Point le plus bas
  - Point d'ouverture
  - Point de fermeture

- **Personnalisation** :
  - Couleur de chaque ligne (paramètres "Couleur ligne...")
  - Épaisseur des lignes
  - Alertes audio configurables pour chaque niveau

### 2. Zone des 30 premières minutes
- **Calcul automatique** au début de chaque journée
- **Trois zones** :
  - Zone initiale (30 premières minutes)
  - Zone reportée au-dessus
  - Zone reportée en-dessous

- **Personnalisation** :
  - Couleur de la zone
  - Opacité
  - Alertes audio

### 3. Profil de volume
- **Affichage automatique** du profil de volume quotidien
- **POC (Point of Control)** :
  - Ligne visible et extensible
  - Couleur et épaisseur personnalisables
  - Alerte audio configurable

- **Zones de faible volume** :
  - Détection automatique des "trous" dans le profil
  - Seuil de volume faible ajustable
  - Alertes audio configurables

## Paramètres configurables

### Style - Niveaux précédents
- Couleur ligne haut précédent
- Couleur ligne bas précédent
- Couleur ligne ouverture précédente
- Couleur ligne clôture précédente
- Épaisseur des lignes

### Alertes - Niveaux précédents
- Activer alerte haut précédent
- Activer alerte bas précédent
- Activer alerte ouverture précédente
- Activer alerte clôture précédente

### Style - Zone 30min
- Couleur zone 30min
- Opacité zone 30min
- Activer alerte zone 30min

### Style - Profil de volume
- Couleur POC
- Épaisseur ligne POC

### Paramètres - Profil de volume
- Seuil volume faible (%)

### Alertes - Profil de volume
- Activer alerte POC
- Activer alerte zones faibles

## Utilisation
1. Ajoutez l'indicateur à votre graphique
2. Ajustez les paramètres selon vos préférences
3. Les niveaux et zones se calculeront automatiquement
4. Les alertes se déclencheront selon vos paramètres

## Notes
- Les calculs commencent après 200 barres pour assurer la précision
- Les zones s'étendent sur 1000 barres par défaut
- Les alertes sont configurables individuellement pour chaque fonctionnalité

## Support
Pour toute question ou problème, veuillez contacter le support technique.

## Version
Version actuelle : 1.0.0
Dernière mise à jour : 09/06/2024 