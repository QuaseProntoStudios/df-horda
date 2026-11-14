using System;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>Rádio 96.6 tocando ao fundo, motor da van e efeitos curtos.</summary>
public partial class GameAudio : Node
{
    private static readonly string[] RadioTracks =
    {
        "Radio/radio_01_frequencia_morta.ogg",
        "Radio/radio_02_bossa_da_madrugada.ogg",
        "Radio/radio_03_asfalto_molhado.ogg",
        "Radio/radio_04_forro_do_entregador.ogg",
        "Radio/radio_05_neon_na_rodovia.ogg",
    };

    private const int SfxVoices = 14;

    private AudioStreamPlayer _radio;
    private AudioStreamPlayer _engine;
    private AudioStreamPlayer[] _sfx;
    private int _nextVoice;
    private int _track;
    private bool _jinglePlayed;
    private readonly Random _random = new();

    public override void _Ready()
    {
        _radio = new AudioStreamPlayer { VolumeDb = -14f, Bus = GameSettings.BusMusic };
        AddChild(_radio);
        _radio.Finished += PlayNextTrack;

        _engine = new AudioStreamPlayer { VolumeDb = -10f, Bus = GameSettings.BusEngine };
        AddChild(_engine);
        _engine.Stream = Assets.Stream("SFX/engine_loop.wav");
        _engine.Finished += RestartEngine;
        RestartEngine();

        _sfx = new AudioStreamPlayer[SfxVoices];
        for (int i = 0; i < SfxVoices; i++)
        {
            _sfx[i] = new AudioStreamPlayer();
            AddChild(_sfx[i]);
        }

        _track = _random.Next(RadioTracks.Length);
        PlayNextTrack();
    }

    public void Play(string relativePath, float volumeDb = -6f, float pitch = 1f, string bus = null)
    {
        AudioStream stream = Assets.Stream(relativePath);
        if (stream == null) return;

        AudioStreamPlayer voice = _sfx[_nextVoice];
        _nextVoice = (_nextVoice + 1) % SfxVoices;
        voice.Stream = stream;
        voice.Bus = bus ?? GameSettings.BusSfx;
        voice.VolumeDb = volumeDb;
        voice.PitchScale = pitch;
        voice.Play();
    }

    /// <summary>Som causado por inimigos (batidas, cuspes, explosões): tem volume próprio nas configurações.</summary>
    public void PlayEnemy(string relativePath, float volumeDb = -6f, float pitch = 1f)
    {
        Play(relativePath, volumeDb, pitch, GameSettings.BusEnemy);
    }

    public void PlayUi(string relativePath, float volumeDb = -6f, float pitch = 1f)
    {
        Play(relativePath, volumeDb, pitch, GameSettings.BusUi);
    }

    /// <summary>speedFraction: 0 parado, 1 na velocidade máxima.</summary>
    public void SetEngine(float speedFraction)
    {
        _engine.PitchScale = 0.75f + 0.9f * Mathf.Clamp(speedFraction, 0f, 1f);
    }

    public void SetEngineRunning(bool running)
    {
        _engine.StreamPaused = !running;
    }

    private void RestartEngine()
    {
        if (_engine.Stream != null) _engine.Play();
    }

    private void PlayNextTrack()
    {
        AudioStream stream;
        if (!_jinglePlayed)
        {
            // A vinheta da rádio abre a partida; depois entram as músicas em sequência.
            _jinglePlayed = true;
            stream = Assets.Stream("Radio/radio_jingle.ogg");
            if (stream == null)
            {
                PlayNextTrack();
                return;
            }
        }
        else
        {
            stream = Assets.Stream(RadioTracks[_track]);
            _track = (_track + 1) % RadioTracks.Length;
        }

        if (stream == null) return;
        _radio.Stream = stream;
        _radio.Play();
    }
}
