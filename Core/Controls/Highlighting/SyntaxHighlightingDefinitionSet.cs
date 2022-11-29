using CarinaStudio.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Set of syntax highlighting definition.
/// </summary>
public sealed class SyntaxHighlightingDefinitionSet
{
    // Fields.
    readonly HashSet<SyntaxHighlightingSpan> attachedSpanDefinitions = new();
    readonly Dictionary<object, HashSet<SyntaxHighlightingToken>> attachedTokenDefinitions = new();
    readonly ObservableList<SyntaxHighlightingSpan> spanDefinitions = new();
    readonly ObservableList<SyntaxHighlightingToken> tokenDefinitions = new();


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingDefinitionSet"/> instance.
    /// </summary>
    /// <param name="name">Name.</param>
    public SyntaxHighlightingDefinitionSet(string name)
    {
        this.Name = name;
        this.spanDefinitions.CollectionChanged += this.OnSpanDefinitionsChanged;
        this.tokenDefinitions.CollectionChanged += this.OnTokenDefinitionsChanged;
    }


    // Attach to given span definition.
    void AttachToSpanDefinition(SyntaxHighlightingSpan spanDefinition)
    {
        if (!this.attachedTokenDefinitions.TryGetValue(spanDefinition, out var attachedTokenDefinitions))
        {
            attachedTokenDefinitions = new();
            this.attachedTokenDefinitions[spanDefinition] = attachedTokenDefinitions;
        }
        else
            throw new InvalidOperationException("Duplicated span definition.");
        spanDefinition.PropertyChanged += this.OnSpanDefinitionPropertyChanged;
        ((INotifyCollectionChanged)spanDefinition.TokenDefinitions).CollectionChanged += this.OnTokenDefinitionsChanged;
        foreach (var tokenDefinition in spanDefinition.TokenDefinitions)
            this.AttachToTokenDefinition(tokenDefinition, attachedTokenDefinitions);
    }


    // Attach to given token definition.
    void AttachToTokenDefinition(SyntaxHighlightingToken tokenDefinition, HashSet<SyntaxHighlightingToken> attachedTokenDefinitions)
    {
        if (!attachedTokenDefinitions.Add(tokenDefinition))
            throw new InvalidOperationException("Duplicated token definition.");
        tokenDefinition.PropertyChanged += this.OnTokenDefinitionPropertyChanged;
    }


    // Detach from given span definition.
    void DetachFromSpanDefinition(SyntaxHighlightingSpan spanDefinition)
    {
        spanDefinition.PropertyChanged -= this.OnSpanDefinitionPropertyChanged;
        ((INotifyCollectionChanged)spanDefinition.TokenDefinitions).CollectionChanged -= this.OnTokenDefinitionsChanged;
        foreach (var tokenDefinition in spanDefinition.TokenDefinitions)
            this.DetachFromTokenDefinition(tokenDefinition, null);
        this.attachedTokenDefinitions.Remove(spanDefinition);
    }


    // Detach from given token definition.
    void DetachFromTokenDefinition(SyntaxHighlightingToken tokenDefinition, HashSet<SyntaxHighlightingToken>? attachedTokenDefinitions)
    {
        tokenDefinition.PropertyChanged -= this.OnTokenDefinitionPropertyChanged;
        attachedTokenDefinitions?.Remove(tokenDefinition);
    }


    /// <summary>
    /// Raised when one of definitions in the set has been changed.
    /// </summary>
    public event EventHandler? Changed;


    /// <summary>
    /// Get name of the set.
    /// </summary>
    public string Name { get; }


    // Raise changed event.
    void OnChanged() =>
        this.Changed?.Invoke(this, EventArgs.Empty);
    

    // Called when property of span definition changed.
    void OnSpanDefinitionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyntaxHighlightingDefinition.IsValid)
            || (sender as SyntaxHighlightingDefinition)?.IsValid == true)
        {
            this.OnChanged();
        }
    }


    // Called when collection of span definitions changed.
    void OnSpanDefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                e.NewItems!.Cast<SyntaxHighlightingSpan>().Let(it =>
                {
                    foreach (var definition in it)
                        this.AttachToSpanDefinition(definition);
                });
                break;
            case NotifyCollectionChangedAction.Remove:
                e.OldItems!.Cast<SyntaxHighlightingSpan>().Let(it =>
                {
                    foreach (var definition in it)
                        this.DetachFromSpanDefinition(definition);
                });
                break;
            case NotifyCollectionChangedAction.Replace:
                e.OldItems!.Cast<SyntaxHighlightingSpan>().Let(it =>
                {
                    foreach (var definition in it)
                        this.DetachFromSpanDefinition(definition);
                });
                e.NewItems!.Cast<SyntaxHighlightingSpan>().Let(it =>
                {
                    foreach (var definition in it)
                        this.AttachToSpanDefinition(definition);
                });
                break;
            case NotifyCollectionChangedAction.Reset:
                foreach (var definition in this.attachedSpanDefinitions.ToArray())
                    this.DetachFromSpanDefinition(definition);
                foreach (var definition in this.spanDefinitions)
                    this.AttachToSpanDefinition(definition);
                break;
        }
        this.OnChanged();
    }


    // Called when property of token definition changed.
    void OnTokenDefinitionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyntaxHighlightingDefinition.IsValid)
            || (sender as SyntaxHighlightingDefinition)?.IsValid == true)
        {
            this.OnChanged();
        }
    }


    // Called when collection of token definitions changed.
    void OnTokenDefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not IList<SyntaxHighlightingToken> tokenDefinitions)
            return;
        var owner = tokenDefinitions == this.tokenDefinitions
            ? (object?)this
            : this.spanDefinitions.FirstOrDefault(it => tokenDefinitions == it.TokenDefinitions);
        if (owner == null || !this.attachedTokenDefinitions.TryGetValue(owner, out var attachedTokenDefinitions))
            return;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                e.NewItems!.Cast<SyntaxHighlightingToken>().Let(it =>
                {
                    foreach (var definition in it)
                        this.AttachToTokenDefinition(definition, attachedTokenDefinitions);
                });
                break;
            case NotifyCollectionChangedAction.Remove:
                e.OldItems!.Cast<SyntaxHighlightingToken>().Let(it =>
                {
                    foreach (var definition in it)
                        this.DetachFromTokenDefinition(definition, attachedTokenDefinitions);
                });
                break;
            case NotifyCollectionChangedAction.Replace:
                e.OldItems!.Cast<SyntaxHighlightingToken>().Let(it =>
                {
                    foreach (var definition in it)
                        this.DetachFromTokenDefinition(definition, attachedTokenDefinitions);
                });
                e.NewItems!.Cast<SyntaxHighlightingToken>().Let(it =>
                {
                    foreach (var definition in it)
                        this.AttachToTokenDefinition(definition, attachedTokenDefinitions);
                });
                break;
            case NotifyCollectionChangedAction.Reset:
                foreach (var definition in attachedTokenDefinitions.ToArray())
                    this.DetachFromTokenDefinition(definition, attachedTokenDefinitions);
                if (owner is SyntaxHighlightingSpan spanDefinition)
                {
                    foreach (var definition in spanDefinition.TokenDefinitions)
                        this.AttachToTokenDefinition(definition, attachedTokenDefinitions);
                }
                else
                {
                    foreach (var definition in this.tokenDefinitions)
                        this.AttachToTokenDefinition(definition, attachedTokenDefinitions);
                }
                break;
        }
        this.OnChanged();
    }
    

    /// <summary>
    /// Get list of span definitions.
    /// </summary>
    public IList<SyntaxHighlightingSpan> SpanDefinitions { get => this.spanDefinitions; }
    

    /// <summary>
    /// Get list of token definitions.
    /// </summary>
    public IList<SyntaxHighlightingToken> TokenDefinitions { get => this.tokenDefinitions; }


    /// <inheritdoc/>
    public override string ToString() =>
        $"{{{this.Name}}}";
}
