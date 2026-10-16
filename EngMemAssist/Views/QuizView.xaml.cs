using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using EngMemAssist.ViewModels;

namespace EngMemAssist.Views;

public partial class QuizView : UserControl
{
    private QuizViewModel? _subscribed;

    public QuizView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Subscribe();
        Unloaded += (_, _) => Unsubscribe();
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (DataContext is QuizViewModel vm)
        {
            _subscribed = vm;
            vm.ShakeRequested += OnShakeRequested;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.ShakeRequested -= OnShakeRequested;
            _subscribed = null;
        }
    }

    /// <summary>Встряска страницы по горизонтали с затуханием.</summary>
    private void OnShakeRequested(object? sender, EventArgs e)
    {
        var animation = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(420) };
        double[] offsets = [0, -12, 11, -9, 7, -4, 2, 0];
        for (var i = 0; i < offsets.Length; i++)
        {
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(
                offsets[i], KeyTime.FromPercent((double)i / (offsets.Length - 1))));
        }

        ShakeTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
    }
}
