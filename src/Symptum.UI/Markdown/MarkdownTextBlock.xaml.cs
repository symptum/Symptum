// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig;
using Symptum.UI.Markdown.Renderers;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown;

[TemplatePart(Name = MarkdownContainerName, Type = typeof(Grid))]
public partial class MarkdownTextBlock : Control
{
    private const string MarkdownContainerName = "MarkdownContainer";
    private Grid? _container;
    private MarkdownPipeline _pipeline;
    private MyFlowDocument _document;
    private WinUIRenderer? _renderer;

    private static readonly DependencyProperty ConfigProperty = DependencyProperty.Register(
        nameof(Config),
        typeof(MarkdownConfig),
        typeof(MarkdownTextBlock),
        new PropertyMetadata(MarkdownConfig.Default, OnConfigChanged)
    );

    private static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnTextChanged));

    public MarkdownConfig Config
    {
        get => (MarkdownConfig)GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock self && e.NewValue != null)
        {
            self.ApplyConfig(self.Config);
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock self && e.NewValue != null)
        {
            self.ApplyText(self.Text, true);
        }
    }

    public MarkdownTextBlock()
    {
        DefaultStyleKey = typeof(MarkdownTextBlock);
        _document = new MyFlowDocument(Config);
        _pipeline = new MarkdownPipelineBuilder()
            .UseAlertBlocks()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseTaskLists()
            .UsePipeTables()
            .Build();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _container = (Grid)GetTemplateChild(MarkdownContainerName);
        _container.Children.Clear();
        _container.Children.Add(_document.StackPanel);
        Build();
    }

    private void ApplyConfig(MarkdownConfig config)
    {
        if (_renderer != null)
        {
            _renderer.Config = config;
        }
    }

    private void ApplyText(string text, bool rerender)
    {
        Markdig.Syntax.MarkdownDocument markdown = Markdig.Markdown.Parse(text ?? string.Empty, _pipeline);
        if (_renderer != null)
        {
            if (rerender)
            {
                _renderer.ReloadDocument();
            }
            _renderer.Render(markdown);
        }
    }

    private void Build()
    {
        if (Config != null)
        {
            if (_renderer == null)
            {
                _renderer = new WinUIRenderer(_document, Config);
            }
            _pipeline.Setup(_renderer);
            ApplyText(Text, false);
        }
    }
}
