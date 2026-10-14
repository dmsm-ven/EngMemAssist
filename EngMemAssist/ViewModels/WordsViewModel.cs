using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;

namespace EngMemAssist.ViewModels;

public sealed partial class WordsViewModel : PageViewModelBase
{
    private readonly IWordRepository _words;
    private readonly ISentenceRepository _sentences;

    private IReadOnlyList<Word> _all = [];

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private IReadOnlyList<Word> _filteredWords = [];

    [ObservableProperty]
    private Word? _selectedWord;

    [ObservableProperty]
    private IReadOnlyList<Sentence> _selectedSentences = [];

    public WordsViewModel(IWordRepository words, ISentenceRepository sentences)
    {
        _words = words;
        _sentences = sentences;
    }

    public override string Title => "Список слов";

    public override async Task OnNavigatedToAsync()
    {
        _all = await _words.GetActiveAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedWordChanged(Word? value) => _ = LoadSentencesAsync(value);

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
