# Music Player MVVM - Laborator 8 (SOLID)
Proiect realizat pentru laboratorul de arhitecturi, integrând pattern-urile Observer, Command și Strategy.

## 📸 Screenshots
*(Am adaugat pozele doveditoare în repository pentru funcționalitățile Now Playing, Strategy Selection și Action History)*

## 🚀 Instrucțiuni de rulare
1. Deschideți soluția în Visual Studio 2026 sau rulați din terminal:
   `cd src/MusicPlayer.App`
   `dotnet run`
2. Folosiți folderul `samples/` din rădăcina repository-ului pentru a testa redarea pieselor audio.

---

## 🧩 Diagrame UML (Mermaid)

### 1. Diagrama de Clase (Pattern-urile integrate)
```mermaid
classDiagram
    %% STRATEGY PATTERN
    class IPlaybackStrategy {
        <<interface>>
        +GetNextTrack()
        +GetPreviousTrack()
    }
    IPlaybackStrategy <|-- SequentialStrategy
    IPlaybackStrategy <|-- ShuffleStrategy
    IPlaybackStrategy <|-- SmartShuffleStrategy
    IPlaybackStrategy <|-- RepeatOneStrategy

    %% COMMAND PATTERN
    class UndoableCommand {
        <<abstract>>
        +Execute()
        +Undo()
    }
    UndoableCommand <|-- PlayCommand
    UndoableCommand <|-- AddTrackCommand
    class CommandHistory {
        +Execute(cmd)
        +Undo()
        +Redo()
    }

    %% OBSERVER PATTERN
    class AudioPlayer {
        +event TrackEnded
        +event PropertyChanged
        +Play()
        +Stop()
    }
    class MainWindowViewModel {
        -UpdateStats()
        -PlayNextTrack()
    }
    AudioPlayer --> MainWindowViewModel : Notifies via Events
    MainWindowViewModel --> IPlaybackStrategy : Uses
    MainWindowViewModel --> CommandHistory : Manages

sequenceDiagram
    actor User
    participant UI as MainWindow
    participant VM as ViewModel
    participant Strat as IPlaybackStrategy
    participant Player as AudioPlayer

    User->>UI: Click "NEXT"
    UI->>VM: Execute NextCmd
    VM->>Strat: GetNextTrack(Playlist, CurrentTrack)
    Strat-->>VM: Return Track (ex: Track 3)
    VM->>Player: Load(Track 3)
    VM->>Player: Play()
    Player-->>UI: PropertyChanged (Update UI / Position)
    Player-->>User: Muzica începe!