using UnityEngine;

public static class GameStateResetter
{
    public static void ResetAllStaticVariables()
    {
        BasicButtonLampGame.ForceGameStateReset = false;
        SimpleDemoManager.IsGameRestarting = false;
    }
}
