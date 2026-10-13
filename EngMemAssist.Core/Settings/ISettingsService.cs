namespace EngMemAssist.Core.Settings;

public interface ISettingsService
{
    AppSettings Current { get; }

    void Save();
}
