using System;

namespace DeadFrequencyHorda;

/// <summary>A página "Configurações", igual no menu principal e na pausa.</summary>
public static class SettingsPages
{
    public static MenuPanel.Page Build(MenuPanel panel, string backPageId, float dimAlpha)
    {
        void GoBack()
        {
            GameSettings.Save();
            panel.Open(backPageId);
        }

        var page = new MenuPanel.Page { Id = "settings", OnBack = GoBack, DimAlpha = dimAlpha };
        page.Title.Add(("CONFIGURAÇÕES", 16, UiStyle.Green));

        page.Items.Add(Slider("Volume geral", () => GameSettings.Master, v => GameSettings.Master = v,
            GameSettings.BusMaster, "UI/ui_click.wav", "Controla todos os sons de uma vez."));
        page.Items.Add(Slider("Música", () => GameSettings.Music, v => GameSettings.Music = v,
            null, null, "A rádio 96.6 e a música do menu."));
        page.Items.Add(Slider("Efeitos", () => GameSettings.Sfx, v => GameSettings.Sfx = v,
            GameSettings.BusSfx, "SFX/delivery_ok.wav", "Motor, armas, sinais e subida de nível."));
        page.Items.Add(Slider("Inimigos", () => GameSettings.Enemy, v => GameSettings.Enemy = v,
            GameSettings.BusEnemy, "SFX/impact_box.wav", "Batidas, cuspes, explosões e dano na van."));
        page.Items.Add(Slider("Interface", () => GameSettings.Ui, v => GameSettings.Ui = v,
            GameSettings.BusUi, "UI/ui_click.wav", "Cliques e sons dos menus."));
        page.Items.Add(Toggle("Tremor de tela", () => GameSettings.Shake, v => GameSettings.Shake = v,
            "Desligue se a câmera tremendo incomodar."));
        page.Items.Add(Toggle("Tela cheia", () => GameSettings.Fullscreen, v =>
        {
            GameSettings.Fullscreen = v;
            GameSettings.ApplyWindow();
        }, "Alterna entre janela e tela cheia."));
        page.Items.Add(MenuPanel.Button("Voltar", GoBack));
        return page;
    }

    private static MenuPanel.Item Slider(string text, Func<float> get, Action<float> set, string previewBus, string previewSound, string hint)
    {
        return new MenuPanel.Item
        {
            Kind = MenuPanel.Kind.Slider, Text = text, Get = get,
            Set = v =>
            {
                set(v);
                GameSettings.ApplyAudio();
            },
            PreviewBus = previewBus, PreviewSound = previewSound,
            Hint = () => hint,
        };
    }

    private static MenuPanel.Item Toggle(string text, Func<bool> get, Action<bool> set, string hint)
    {
        return new MenuPanel.Item
        {
            Kind = MenuPanel.Kind.Toggle, Text = text,
            Get = () => get() ? 1f : 0f,
            Set = v => set(v > 0.5f),
            Hint = () => hint,
        };
    }
}
