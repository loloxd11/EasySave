#  EasySave - Logiciel de sauvegarde de fichiers

EasySave est un logiciel de sauvegarde de fichiers développé dans le cadre d’un projet encadré par **ProSoft**, sous la responsabilité du DSI. Ce projet a été confié à une équipe de 3 étudiants en informatique.

---

## Présentation de ProSoft

ProSoft est un éditeur de logiciels professionnels. Le projet **EasySave** s’inscrit dans la Suite ProSoft et sera distribué à des clients professionnels selon la politique tarifaire suivante :

- **Prix unitaire** : 200 € HT  
- **Contrat de maintenance annuel 5/7 (8h-17h)** : 12 % du prix d’achat  
  _(Contrat reconduit tacitement avec revalorisation basée sur l’indice SYNTEC)_

L’équipe en charge du projet EasySave doit :

- Développer le logiciel
- Gérer les versions majeures et mineures
- Rédiger la documentation :
  - **Utilisateur** : manuel d’utilisation (1 page)
  - **Support technique** : emplacement par défaut, configuration minimale, chemins de configuration…

---

## Environnement de travail & Outils imposés

| Outil                  | Usage                                    |
|------------------------|------------------------------------------|
| **Visual Studio 2022+**| Développement                           |
| **GitHub**             | Versioning, travail collaboratif         |
| **ArgoUML**            | Diagrammes UML (modélisation)            |


---

## Technologies utilisées

- **Langage** : C#  
- **Framework** : .NET 8.0  
- **Type d'application** : Console (v1) et WPF (v2)

---
## Version 1.0

La première version est une application console en .NET Core permettant la création de **5 jobs de sauvegarde maximum**.

---

## Version 1.1

La version 1.1 apporte des améliorations à l’application console. Elle introduit la possibilité de choisir le format de log entre **JSON** et **XML**.  
L’interface console est maintenue et continue d’être supportée.

---

## Une tâche de sauvegarde est définie par :

- Un nom
- Un répertoire source
- Un répertoire cible
- Un type de sauvegarde :  
  - Complet  
  - Différentiel

Le logiciel doit être **entièrement compréhensible en anglais et français**.

---

## Exécution

L'utilisateur peut lancer une ou plusieurs tâches de sauvegarde, exécutées **séquentiellement**.  
Les répertoires source et cible peuvent se trouver localement ou en réseau.  
Tous les fichiers du répertoire source sont pris en compte.

---

## Fichier journal (log)

Chaque exécution écrit un fichier journal contenant l’historique des actions. Les informations minimales attendues sont :

- Horodatage  
- Nom de la tâche  
- Adresse du fichier source (format UNC)  
- Adresse du fichier de destination (format UNC)  
- Taille du fichier  
- Temps de transfert  
- État (succès ou erreur)

---

## Fichier d’état (state)

Permet un suivi en temps réel d’une tâche en cours :

- Horodatage  
- Nom de la tâche  
- État de la tâche (ex. : Active, Inactif, etc.)  
- Nombre de fichiers restants  
- Nombre de fichiers déjà traités  
- Taille des fichiers traités/restants  



## Bonnes pratiques et normes de développement

Pour garantir la maintenabilité, la lisibilité et la réutilisabilité du code, les règles suivantes sont imposées :

- Tous les documents, noms de variables, commentaires et messages doivent être **en anglais**
- Fonctions de taille raisonnable
- Éviter la duplication de code (copier-coller proscrit)
- Respect des conventions de nommage
- Code structuré pour être repris rapidement
- Release notes obligatoires pour chaque version

---

## Documentation

- anuel utilisateur : 1 page, simple et accessible
- Fiche support : chemins d'installation, prérequis système, fichiers de configuration

---

## Contraintes supplémentaires

- Le projet doit être structuré pour **réduire les coûts de développement** des futures versions
- Être capable de **réagir rapidement** en cas de dysfonctionnements
- L’interface utilisateur (UI) doit être **soignée et professionnelle**
- L’ensemble du projet doit pouvoir être **repris facilement** par une autre équipe

---

##  Livrables attendus

- ✅ Environnement de développement conforme aux normes de ProSoft
- ✅ Gestion de version Git rigoureuse :
  - Historique clair
  - Commits explicites
  - Branches bien organisées
- ✅ Diagrammes UML à remettre **24h avant chaque livrable**
- ✅ Code :
  - Sans redondance inutile
  - Bien structuré
  - Conforme aux conventions
- ✅ Documentation complète (utilisateur + support)
- ✅ Release notes pour chaque version

---

## Suivi du projet

Ce projet sera évalué en continu sur :

- La **qualité du code**
- Le **respect des contraintes imposées**
- La **rigueur dans la gestion de versions**
- La **qualité des livrables techniques et documentaires**

---

## Équipe de développement

- CAVALCA Kyllian 
- LECLERC-APPERE Loan 
- MAWUNU Akouwa Nathalie 

---

## 🔗 Liens utiles

- [Visual Studio Community](https://visualstudio.microsoft.com/fr/vs/)
- [ArgoUML](http://argouml.tigris.org/)
