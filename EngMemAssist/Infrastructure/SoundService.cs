using System.IO;
using System.Media;
using EngMemAssist.Core.Settings;

namespace EngMemAssist.Infrastructure;

public enum SoundEffect
{
    Correct,
    Wrong,
    Next,
    Fanfare,
}

public interface ISoundService
{
    /// <summary>Проигрывает эффект, если звук включён в настройках. Ошибки воспроизведения игнорируются.</summary>
    void Play(SoundEffect effect);
}

/// <summary>
/// Короткие тихие звуки, сгенерированные в памяти (файлы в проект не нужны): синус с плавной огибающей.
/// </summary>
public sealed class SoundService : ISoundService
{
    private const int SampleRate = 22050;

    private readonly ISettingsService _settings;
    private readonly Dictionary<SoundEffect, SoundPlayer> _players = [];

    public SoundService(ISettingsService settings)
    {
        _settings = settings;
    }

    public void Play(SoundEffect effect)
    {
        if (!_settings.Current.SoundEnabled)
        {
            return;
        }

        try
        {
            if (!_players.TryGetValue(effect, out var player))
            {
                player = new SoundPlayer(new MemoryStream(Synthesize(effect)));
                _players[effect] = player;
            }

            player.Play();
        }
        catch (Exception)
        {
            // Нет звукового устройства или поток недоступен: звук не критичен.
        }
    }

    /// <summary>Ноты: частота (Гц), длительность (мс), громкость 0..1.</summary>
    private static (double Freq, int Ms, double Volume)[] Notes(SoundEffect effect) => effect switch
    {
        SoundEffect.Correct => [(1046.5, 70, 0.22), (1318.5, 120, 0.22)],
        SoundEffect.Wrong => [(196, 130, 0.28), (155.6, 170, 0.28)],
        SoundEffect.Fanfare => [(523.3, 110, 0.25), (659.3, 110, 0.25), (784, 110, 0.25), (1046.5, 380, 0.25)],
        _ => [],
    };

    /// <summary>
    /// «Вжух» ветра: белый шум через полосовой фильтр, центр которого скользит вверх и вниз,
    /// с нарастанием и затуханием громкости. Длительность примерно как у анимации смены вопроса.
    /// </summary>
    private static List<short> Whoosh()
    {
        const int ms = 480;
        const double volume = 0.30;
        var count = SampleRate * ms / 1000;
        var samples = new List<short>(count);
        var random = new Random(7);
        double low = 0, band = 0;

        for (var i = 0; i < count; i++)
        {
            var progress = (double)i / count;

            // Частота фильтра: поднимается к середине и опускается (эффект пролетающего потока).
            var center = 350 + 1900 * Math.Sin(Math.PI * Math.Pow(progress, 0.8));
            var f = 2 * Math.Sin(Math.PI * center / SampleRate);
            const double damping = 0.45; // чем меньше, тем «свистящее»

            var noise = random.NextDouble() * 2 - 1;
            low += f * band;
            var high = noise - low - damping * band;
            band += f * high;

            // Плавное нарастание (~35%) и мягкое затухание.
            var envelope = progress < 0.35
                ? Math.Pow(progress / 0.35, 1.5)
                : Math.Pow(1 - (progress - 0.35) / 0.65, 1.8);

            var value = Math.Clamp(band * 0.9 * envelope * volume, -1.0, 1.0);
            samples.Add((short)(value * short.MaxValue));
        }

        return samples;
    }

    private static byte[] Synthesize(SoundEffect effect)
    {
        var samples = effect == SoundEffect.Next ? Whoosh() : new List<short>();

        foreach (var (freq, ms, volume) in Notes(effect))
        {
            var count = SampleRate * ms / 1000;
            for (var i = 0; i < count; i++)
            {
                var t = (double)i / SampleRate;
                var progress = (double)i / count;

                // Быстрая атака (5 мс) и плавное затухание, чтобы не было щелчков.
                var attack = Math.Min(1.0, t / 0.005);
                var decay = Math.Pow(1.0 - progress, 1.6);

                var wave = Math.Sin(2 * Math.PI * freq * t) + 0.25 * Math.Sin(4 * Math.PI * freq * t);
                samples.Add((short)(wave / 1.25 * attack * decay * volume * short.MaxValue));
            }
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var dataLength = samples.Count * 2;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);          // PCM
        writer.Write((short)1);          // моно
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);    // байт в секунду
        writer.Write((short)2);          // байт на отсчёт
        writer.Write((short)16);         // бит на отсчёт
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);
        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
