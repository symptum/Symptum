using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueParameter : ObservableObject
{
    #region Properties

    private string title = string.Empty;

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    private List<ReferenceValueEntry>? entries;

    public List<ReferenceValueEntry>? Entries
    {
        get => entries;
        set => SetProperty(ref entries, value);
    }

    #endregion

    public ReferenceValueParameter()
    {
    }
}
