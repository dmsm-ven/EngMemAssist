namespace EngMemAssist.Infrastructure;

/// <summary>
/// Один экземпляр приложения: автозапуск + ручной запуск не должны давать два трея и два обращения к базе.
/// Второй экземпляр будит первый (тот показывает окно) и завершается.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activate;

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(initiallyOwned: true, "EngMemAssist.SingleInstance.Mutex", out var created);
        IsFirst = created;
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, "EngMemAssist.SingleInstance.Activate");
    }

    public bool IsFirst { get; }

    public void SignalFirstInstance() => _activate.Set();

    /// <summary>Фоновый поток ждёт сигнала от второго экземпляра; onActivate вызывается НЕ в потоке UI.</summary>
    public void ListenForActivation(Action onActivate)
    {
        var thread = new Thread(() =>
        {
            while (_activate.WaitOne())
            {
                onActivate();
            }
        })
        {
            IsBackground = true,
            Name = "EngMemAssist.ActivationListener",
        };
        thread.Start();
    }

    public void Dispose()
    {
        try
        {
            if (IsFirst)
            {
                _mutex.ReleaseMutex();
            }
        }
        catch (ApplicationException)
        {
            // Мьютекс принадлежит другому потоку: при выходе процесса система освободит его сама.
        }

        _mutex.Dispose();
        _activate.Dispose();
    }
}
