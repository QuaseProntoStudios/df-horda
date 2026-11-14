namespace DeadFrequencyHorda;

/// <summary>
/// Versão do jogo. Ao lançar uma versão nova: mude Current, adicione a entrada no topo do
/// CHANGELOG.md e leve para a main (por pull request); o GitHub Actions compila, publica o Release e atualiza o site.
/// Suffix fica vazio; o build beta do CI troca por "-beta.N".
/// </summary>
public static class GameVersion
{
    public const string Current = "0.3.1";
    public const string Suffix = "";

    public static string Full => Current + Suffix;
}
