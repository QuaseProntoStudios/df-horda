namespace DeadFrequencyHorda;

/// <summary>
/// Versão do jogo. Ao lançar uma versão nova: mude Current, adicione a entrada no topo do
/// CHANGELOG.md e dê push na main; o GitHub Actions compila, publica o Release e atualiza o site.
/// </summary>
public static class GameVersion
{
    public const string Current = "0.2.0";
}
