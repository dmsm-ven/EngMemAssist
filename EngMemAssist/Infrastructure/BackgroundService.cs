using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Settings;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EngMemAssist.Infrastructure;

public sealed record ColorPreset(string Name, string Hex);

/// <summary>Фон приложения: картинка из %AppData% поверх цветной подложки. Все значения берутся из настроек.</summary>
public sealed partial class AppBackgroundService : ObservableObject
{
    public static readonly ColorPreset[] Presets =
    [
        new("Чёрный", "#000000"),
        new("Тёмно-серый", "#1E1E1E"),
        new("Тёмно-синий", "#0B1633"),
        new("Тёмно-фиолетовый", "#1A1033"),
        new("Белый", "#FFFFFF"),
    ];

    private readonly ISettingsService _settings;

    [ObservableProperty]
    private ImageSource? _image;

    [ObservableProperty]
    private double _imageOpacity;

    [ObservableProperty]
    private Brush _overlayBrush = Brushes.Black;

    public AppBackgroundService(ISettingsService settings)
    {
        _settings = settings;
        Reload();
    }

    public bool HasImage => Image is not null;

    partial void OnImageChanged(ImageSource? value) => OnPropertyChanged(nameof(HasImage));

    /// <summary>Копирует выбранный файл в папку приложения и включает его как фон.</summary>
    public void SetImage(string sourcePath)
    {
        AppPaths.EnsureCreated();
        var fileName = "background" + Path.GetExtension(sourcePath).ToLowerInvariant();
        var target = AppPaths.BackgroundFile(fileName);

        // Старый файл (возможно с другим расширением) удаляем, чтобы не копились.
        RemoveFile();

        File.Copy(sourcePath, target, overwrite: true);
        _settings.Current.BackgroundImageFile = fileName;
        _settings.Save();
        Reload();
    }

    public void ClearImage()
    {
        RemoveFile();
        _settings.Current.BackgroundImageFile = null;
        _settings.Save();
        Reload();
    }

    public void SetOpacity(double value)
    {
        _settings.Current.BackgroundOpacity = Math.Clamp(value, 0, 1);
        _settings.Save();
        ImageOpacity = _settings.Current.BackgroundOpacity;
    }

    public void SetOverlayColor(string hex)
    {
        _settings.Current.BackgroundOverlayColor = hex;
        _settings.Save();
        OverlayBrush = ParseBrush(hex);
    }

    public void Reload()
    {
        var s = _settings.Current;
        ImageOpacity = Math.Clamp(s.BackgroundOpacity, 0, 1);
        OverlayBrush = ParseBrush(s.BackgroundOverlayColor);
        Image = LoadImage(s.BackgroundImageFile);
    }

    private void RemoveFile()
    {
        try
        {
            if (!string.IsNullOrEmpty(_settings.Current.BackgroundImageFile))
            {
                // Картинку нужно отпустить до удаления: файл читается с OnLoad, но на всякий случай.
                Image = null;
                var path = AppPaths.BackgroundFile(_settings.Current.BackgroundImageFile);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
        catch (IOException)
        {
        }
    }

    private static ImageSource? LoadImage(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var path = AppPaths.BackgroundFile(fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // не держим файл открытым
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception)
        {
            return null; // битый файл — просто без фона
        }
    }

    private static Brush ParseBrush(string? hex)
    {
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex ?? "#000000"));
            brush.Freeze();
            return brush;
        }
        catch (FormatException)
        {
            return Brushes.Black;
        }
    }
}
