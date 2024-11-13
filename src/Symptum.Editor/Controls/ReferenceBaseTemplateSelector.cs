using Symptum.Core.Data.Bibliography;

namespace Symptum.Editor.Controls;

public partial class ReferenceBaseTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BookReferenceTemplate { get; set; }

    public DataTemplate? LinkReferenceTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        DataTemplate? template = null;
        if (item is ListEditorItemWrapper<ReferenceBase> wrapper &&
            wrapper.Value is ReferenceBase reference)
        {
            template = reference switch
            {
                BookReference => BookReferenceTemplate,
                LinkReference => LinkReferenceTemplate,
                _ => null,
            };
        }

        return template ?? base.SelectTemplateCore(item);
    }
}
