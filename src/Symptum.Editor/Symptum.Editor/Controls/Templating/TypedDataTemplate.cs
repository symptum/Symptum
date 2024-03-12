using Microsoft.UI.Xaml.Markup;

namespace Symptum.Editor.Controls.Templating;

[ContentProperty(Name = nameof(DataTemplate))]
public class TypedDataTemplate
{
    #region Properties

    public Type DataType { get; set; }

    public DataTemplate DataTemplate { get; set; }

    #endregion
}
