using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Models;

namespace EngMemAssist.ViewModels;

/// <summary>Один пункт выбора стратегии квиза (radio-кнопка на главной).</summary>
public sealed partial class StrategyOption : ObservableObject
{
    private readonly Action<StrategyOption> _onSelected;

    [ObservableProperty]
    private bool _isSelected;

    public StrategyOption(QuizStrategy strategy, bool isSelected, Action<StrategyOption> onSelected)
    {
        Strategy = strategy;
        _isSelected = isSelected;
        _onSelected = onSelected;
    }

    public QuizStrategy Strategy { get; }

    public string Title => Strategy.Title();

    public string Description => Strategy.Description();

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            _onSelected(this);
        }
    }
}
