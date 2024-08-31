using System.Collections.ObjectModel;
using Symptum.Core.Extensions;

namespace Symptum.Core.Subjects;

public class SubjectsManager
{
    public static ObservableCollection<Subject> Subjects { get; } = [];

    public static void RegisterSubject(Subject subject)
    {
        Subjects.AddItemToListIfNotExists(subject);
    }

    public static void UnregisterSubject(Subject subject)
    {
        Subjects.RemoveItemFromListIfExists(subject);
    }
}
