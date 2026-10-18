using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;
using EngMemAssist.Core.Services;
using EngMemAssist.Navigation;
using System.Windows;

namespace EngMemAssist.ViewModels;

public sealed partial class WordsViewModel : PageViewModelBase
{
    private readonly IWordRepository _words;
    private readonly ISentenceRepository _sentences;
    private readonly ManualSentenceService _manual;

    private IReadOnlyList<Word> _all = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private IReadOnlyList<Word> _filteredWords = [];

    [ObservableProperty]
    private Word? _selectedWord;

    [ObservableProperty]
    private IReadOnlyList<Sentence> _selectedSentences = [];

    /// <summary>Открыто поле ввода своего предложения (кнопка «+» превращается в галку).</summary>
    [ObservableProperty]
    private bool _isAddingSentence;

    [ObservableProperty]
    private string _newSentenceText = "";

    public WordsViewModel(IWordRepository words, ISentenceRepository sentences, ManualSentenceService manual)
    {
        _manual = manual;
        _words = words;
        _sentences = sentences;
    }

    public override string Title => "Список слов";

    public override NavSection Section => NavSection.Words;

    public override async Task OnNavigatedToAsync()
    {
        IsLoading = true;
        try
        {
            _all = await _words.GetActiveAsync();

            var counts = await _sentences.CountByWordAsync();
            foreach (var word in _all)
            {
                word.SentenceCount = counts.GetValueOrDefault(word.Id);
            }

            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedWordChanged(Word? value)
    {
        CancelAddSentence();
        AddSentenceCommand.NotifyCanExecuteChanged();
        _ = LoadSentencesAsync(value);
    }

    /// <summary>«+» открывает поле ввода, галка (та же кнопка) сохраняет предложение.</summary>
    [RelayCommand(CanExecute = nameof(CanAddSentence))]
    private async Task AddSentenceAsync()
    {
        if (SelectedWord is not { } word)
        {
            return;
        }

        if (!IsAddingSentence)
        {
            IsAddingSentence = true;
            return;
        }

        try
        {
            await _manual.AddAsync(word, NewSentenceText);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "EngMemAssist", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить предложение:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sentences = await _sentences.GetByWordIdsAsync([word.Id]);
        word.SentenceCount = sentences.Count;
        if (SelectedWord?.Id == word.Id)
        {
            SelectedSentences = sentences;
        }

        CancelAddSentence();
    }

    private bool CanAddSentence() => SelectedWord is not null;

    [RelayCommand]
    private void CancelAddSentence()
    {
        IsAddingSentence = false;
        NewSentenceText = "";
    }

    private void ApplyFilter()
    {
        var text = SearchText.Trim();
        FilteredWords = text.Length == 0
            ? _all
            : _all.Where(w =>
                    w.English.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    w.Translation.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    private async Task LoadSentencesAsync(Word? word)
    {
        if (word is null)
        {
            SelectedSentences = [];
            return;
        }

        try
        {
            var sentences = await _sentences.GetByWordIdsAsync([word.Id]);
            if (SelectedWord?.Id == word.Id)
            {
                SelectedSentences = sentences;
            }
        }
        catch
        {
            SelectedSentences = [];
        }
    }
}
