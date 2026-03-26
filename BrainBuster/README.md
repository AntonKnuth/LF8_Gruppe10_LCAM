# 🧠 Brain Buster - Quiz Game

Ein cooles Quiz-Spiel mit verschiedenen Kategorien, Punktesystem und Rangliste!

## Features

- **Quiz-Kategorien**: Verschiedene Themengebiete zum Auswählen
- **Punktesystem**: Punkte für richtige Antworten + Bonus für schnelle Reaktion
- **Rangliste**: Vergleich mit anderen Spielern
- **Solo & Multiplayer**: Alleine oder gegen Freunde spielen
- **Admin-Backend**: Eigene Fragen hinzufügen und verwalten
- **Achievements**: Belohnungen für besondere Leistungen

## Schnellstart

### 1. Projekt bauen
```bash
dotnet build
```

### 2. Server starten
```bash
dotnet run
```

### 3. Im Browser öffnen
```
http://localhost:8080
```

## Konsolenversion

```bash
dotnet run -- console
```

**Steuerungshilfe**: `dotnet run -- h`

## Projektstruktur

```
BrainBuster/
├── Program.cs          # Einstiegspunkt
├── WebServer.cs        # HTTP Server
├── ConsoleGame.cs      # Konsolenversion
├── Database.cs         # SQLite Datenbank
├── QuizApi.cs          # OpenTDB API
├── Models/             # Datenmodelle
│   ├── Question.cs
│   ├── Player.cs
│   ├── Category.cs
│   └── Achievement.cs
├── Tests/              # Unit Tests
│   └── QuizTests.cs
├── wwwroot/            # Frontend Dateien
│   ├── index.html
│   ├── game.html
│   ├── admin.html
│   ├── style.css
│   └── script.js
└── brainbuster.db      # SQLite Datenbank
```

## Tech Stack

- **Backend**: C# mit HttpListener (kein Framework!)
- **Frontend**: HTML, CSS, JavaScript (vanilla - keine Frameworks!)
- **Datenbank**: SQLite
- **Quiz-API**: OpenTDB (für externe Fragen)

## Entwickelt von

Ein Azubi-Projekt 😎
