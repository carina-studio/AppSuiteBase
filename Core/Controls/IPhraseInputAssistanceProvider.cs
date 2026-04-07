using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Interface to provide functions for phrase input assistance.
/// </summary>
public interface IPhraseInputAssistanceProvider
{
    /// <summary>
    /// Select candidate phrases to assist user input.
    /// </summary>
    /// <param name="prefix">Prefix to select phrases.</param>
    /// <param name="postfix">Postfix to select phrases.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of selecting candidate phrases.</returns>
    Task<IList<string>> SelectCandidatePhrasesAsync(string prefix, string? postfix, CancellationToken cancellationToken);
}


/// <summary>
/// Event data for candidate input phrase selected by user.
/// </summary>
/// <param name="phrase">The selected phrase.</param>
public class CandidateInputPhraseSelectedEventArgs(string phrase) : CancelEventArgs
{
    /// <summary>
    /// Get the selected phrase.
    /// </summary>
    public string Phrase { get; } = phrase;
}