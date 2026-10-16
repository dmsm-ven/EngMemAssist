using EngMemAssist.Core.Models;

namespace EngMemAssist.ViewModels;

/// <summary>Плитка выбора стратегии квиза на главной.</summary>
public sealed class StrategyOption
{
    public StrategyOption(QuizStrategy strategy)
    {
        Strategy = strategy;
    }

    public QuizStrategy Strategy { get; }

    public string Title => Strategy.Title();

    public string Description => Strategy.Description();

    public string Icon => Strategy.Icon();
}
