namespace EngMemAssist.Core.Abstractions;

/// <summary>Приводит английское слово к нормальной форме (children -> child).</summary>
public interface ILemmatizer
{
    string Lemmatize(string word);
}
