namespace DeadFrequencyHorda;

/// <summary>Estado que atravessa a troca de cena (menu para partida).</summary>
public static class Session
{
    public const string MenuScene = "res://Scenes/main_menu.tscn";
    public const string GameScene = "res://Scenes/main.tscn";

    public static int Difficulty = 1;
    public static bool ContinueRun;
}
