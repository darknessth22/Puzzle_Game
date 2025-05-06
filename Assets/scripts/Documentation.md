# Game Scripts Documentation

## Overview
This document provides a comprehensive overview of the scripts used in the Light Shifting puzzle game, explaining their purpose, functionality, and interactions. The game is a memory-based button-lamp matching puzzle where players must remember and recreate a demonstrated sequence of button-to-lamp connections.

## Major Game Functionality

### Core Gameplay
1. Players enter their name and view instructions before starting
2. A demonstration sequence shows which buttons correspond to which lamps
3. Players must press buttons and their corresponding lamps in the correct sequence
4. Correct matches turn green, incorrect matches turn red
5. Players have a limited number of tries (default: 10) to complete the puzzle
6. Game tracks progress and provides analytics on performance

### Key Features
- Non-random, fixed sequence for each game session
- Configurable button-to-lamp mappings through the Unity Inspector
- Animated buttons with sound effects during gameplay and demo
- Win/lose screens with sound effects and animated text
- Analytics panel showing performance data
- Background music system with scene-specific audio
- Custom cursor system
- Multi-panel UI flow with transitions

## Game Flow
1. **Main Menu**: Player selects "Play" to begin
2. **Player Name Panel**: Player enters their name
3. **Introduction Panel**: Shows game context without audio
4. **Instructions Panel**: Explains gameplay with audio
5. **Pre-Game Panel**: Brief message before gameplay begins
6. **Demo Phase**: Shows button-lamp connections automatically
7. **Gameplay Phase**: Player attempts to recreate the connections
8. **Results Phase**: Win or lose panel with analytics option

## Core Scripts

### BasicButtonLampGame.cs
The main game controller that manages:
- Game state (gameplay, win/lose conditions)
- Button and lamp interactions and matching logic
- Try counting and progress tracking
- UI panels (win, lose, analytics)
- Scene transitions (retry, main menu)
- Sound effects for correct/incorrect matches
- Visual feedback (color changes) for matches

### SimpleDemoManager.cs
Manages the demonstration phase:
- Controls timing and sequence of the demo
- Highlights button-lamp pairs to teach the player
- Tracks demo state (running, completed)
- Plays button animations and sounds during demo
- Shows post-demo message with sound
- Works with ButtonNumberMapping for correct associations
- Supports configurable timing parameters

### ButtonNumberMapping.cs
Defines the relationships between buttons and lamps:
- Stores button-lamp pairs in a serializable list for Inspector configuration
- Provides methods to find buttons by number and vice versa
- Supports dynamic mapping creation and modification
- Used by both demo and gameplay systems
- Eliminates need for hardcoded button-lamp relationships

### BasicButton.cs
Handles individual button behavior:
- Detects mouse clicks
- Plays sound effects and animations
- Notifies the game manager when clicked
- Supports Inspector-configured audio settings
- Automatically finds game manager references

### BasicNumber.cs
Manages individual lamp objects:
- Detects mouse clicks
- Notifies the game manager when clicked
- Stores lamp number value
- Automatically finds game manager references

### PlayerNameAndInstructions.cs
Manages the pre-game flow:
- Main menu panel with play and exit buttons
- Player name input and storage
- Introduction panel without audio
- Instructions panel with audio
- Pre-game panel with timed display
- Background music management
- Scene transitions to the main game
- Cursor visibility management

### CustomCursor.cs
Handles the custom cursor system:
- Sets and maintains custom cursor texture
- Ensures cursor remains visible
- Provides methods to toggle between custom and default cursors
- Supports configurable hotspot (click point)

### GameInitializer.cs
Handles game initialization:
- Sets up the custom cursor
- Ensures cursor visibility
- Initializes other game systems
- Provides consistent cursor behavior across scenes

### GameStateResetter.cs
Manages game state between sessions:
- Resets static variables to prevent state persistence
- Ensures clean state when restarting the game
- Prevents UI corruption during scene transitions

## Audio System

### Background Music
- Main menu features background music that plays continuously
- Music fades out during transitions to other panels
- Different audio can be played for different game phases

### Sound Effects
- **Button Sounds**: Each button plays a click sound when pressed
- **Match Sounds**: Correct and incorrect matches play distinct sounds
- **Result Sounds**: Win and lose conditions have unique sound effects
- **Instruction Sound**: Plays when the instruction panel is shown
- **Demo Completion Sound**: Plays after the demo sequence finishes

### Volume Control
- Separate volume controls for different sound types
- Configurable through the Inspector
- Higher volume for gameplay feedback sounds

## UI System

### Panel Flow
The game uses a series of panels that appear in sequence:
1. **Main Menu Panel**: Entry point with Play and Exit buttons
2. **Player Name Panel**: Collects player name via input field
3. **Introduction Panel**: Provides game context
4. **Instructions Panel**: Explains gameplay mechanics
5. **Pre-Game Panel**: Brief message before gameplay begins
6. **Game UI**: Shows tries remaining and progress during gameplay
7. **Win/Lose Panels**: Display game results with retry options
8. **Analytics Panel**: Shows detailed performance data

### Text Elements
- TextMeshPro components used throughout
- Animated text for win/lose messages
- Configurable text content through the Inspector
- Support for cursor sprite images in instruction text

### Analytics Display
- Shows performance data in two columns
- Includes try number and wrong button count
- Supports scrolling for multiple tries
- Accessible via ESC key from win/lose panels

## Configuration Options

### Inspector-Based Configuration
The game uses Unity's Inspector extensively for configuration:
- **Button-Lamp Mappings**: Define which buttons correspond to which lamps
- **Audio Settings**: Configure sounds and volumes
- **UI Text**: Set text content for instructions and messages
- **Timing Parameters**: Adjust delays and durations for demo and transitions
- **Game Settings**: Set maximum tries and other gameplay parameters

### Demo Sequence Configuration
- Configure timing of the demo sequence
- Set delays between button highlights
- Define post-demo message and display duration
- Option to use all buttons or a specific sequence

### Visual Feedback Configuration
- Configure materials for correct/incorrect matches
- Set emission intensity for highlighted objects
- Define color changes for feedback

## Implementation Notes

### Object References
- Scripts use automatic reference finding where possible
- GameObject tags (Button, LampBase, UIPanel, etc.) used for identification
- Inspector-based references preferred over hardcoded values

### Scene Management
- Game uses multiple scenes (MainMenu, PuzzleGame)
- Scene transitions handle cursor visibility and game state reset
- Retry functionality reloads the current scene with clean state

### State Management
- Static variables track game state across scene loads
- GameStateResetter ensures clean state when restarting
- ForceGameStateReset flag prevents interaction during transitions