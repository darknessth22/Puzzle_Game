using UnityEngine;

public static class GameStateResetter
{
    // This class is used to reset all static variables in the game
    // It's called when starting a new game from the main menu

    public static void ResetAllStaticVariables()
    {
        Debug.Log("Resetting all static variables");

        // Reset static variables in BasicButtonLampGame
        BasicButtonLampGame.ForceGameStateReset = false;

        // Reset static variables in SimpleDemoManager
        SimpleDemoManager.IsGameRestarting = false;

        // Reset static variables in PlayerNameAndInstructions
        // (None to reset currently)

        Debug.Log("All static variables have been reset");
    }
}
