using System.Collections.ObjectModel;
using Symptum.Common.ProjectSystem;
using Symptum.Core.Data;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Editor.EditorPages;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Editor.Controls;

public sealed partial class ResourcePropertiesEditorControl : UserControl
{
    private readonly ObservableCollection<ListEditorItemWrapper<AuthorInfo>> _packageAuthors = [];
    private readonly ObservableCollection<string> _packageTags = [];

    private readonly ObservableCollection<ListEditorItemWrapper<AuthorInfo>> _fileAuthors = [];
    private readonly ObservableCollection<string> _fileTags = [];

    public ResourcePropertiesEditorControl()
    {
        InitializeComponent();
        parentResourceButton.Click += (s, e) => OpenParentResource();
        HandleListEditors();
    }

    #region Properties

    public static readonly DependencyProperty ResourceProperty =
    DependencyProperty.Register(
        nameof(Resource),
        typeof(IResource),
        typeof(ResourcePropertiesEditorControl),
        new(null, OnResourceChanged));

    private static void OnResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResourcePropertiesEditorControl propertiesEditor)
        {
            propertiesEditor.SetResource(e.NewValue as IResource);
        }
    }

    public IResource? Resource
    {
        get => GetValue(ResourceProperty) as IResource;
        set => SetValue(ResourceProperty, value);
    }

    #endregion

    private void SetResource(IResource? resource)
    {
        if (resource != null)
        {
            LoadResource(resource);
        }
        else
            ClearResource();
    }

    private void ClearResource()
    {
        typeTB.Text = null;
        parentResourceButton.Visibility = Visibility.Collapsed;
        parentResourceButton.Content = null;
        titleTB.Text = null;
        idTB.Text = null;
        uriTB.Text = null;
        idTB.Visibility = Visibility.Visible;
        uriTB.Visibility = Visibility.Visible;
        genButton.Visibility = Visibility.Visible;
        scCB.Visibility = Visibility.Collapsed;
        resourceTreeExpander.Visibility = Visibility.Collapsed;
        childrenResIR.ItemsSource = null;
        packageExpander.Visibility = Visibility.Collapsed;
        packageDescriptionTB.Text = null;
        packageVersionTB.Text = null;
        _packageAuthors.LoadFromList(null);
        _packageTags.Clear();
        metadataExpander.Visibility = Visibility.Collapsed;
        splitMDCB.IsChecked = null;
        mdPathTB.Text = null;
        fileExpander.Visibility = Visibility.Collapsed;
        fileTypeTB.Text = null;
        filePathTB.Text = null;
        fileDescriptionTB.Text = null;
        _fileAuthors.LoadFromList(null);
        _fileTags.Clear();
    }

    public void ResetResource() => LoadResource(Resource);

    private void LoadResource(IResource? resource)
    {
        if (resource == null) return;

        typeTB.Text = "Resource Type: " + resource.GetType().ToString();
        if (resource.ParentResource != null)
        {
            parentResourceButton.Visibility = Visibility.Visible;
            parentResourceButton.Content = "Parent Resource: " + (resource.ParentResource?.Id ?? resource.ParentResource?.Title);
        }
        titleTB.Text = resource.Title;

        idTB.Text = resource.Id;
        uriTB.Text = resource.Uri?.ToString();

        if (resource.CanHandleChildren && resource.ChildrenResources != null)
        {
            resourceTreeExpander.Visibility = Visibility.Visible;
            childrenResIR.ItemsSource = resource.ChildrenResources;
        }

        if (resource is PackageResource package)
        {
            packageExpander.Visibility = Visibility.Visible;
            packageDescriptionTB.Text = package.Description;
            packageVersionTB.Text = package.Version?.ToString();
            _packageAuthors.LoadFromList(package.Authors);
            _packageTags.Clear();
            _packageTags.AddRange(package.Tags);
        }
        else if (resource is MetadataResource metadataResource)
        {
            metadataExpander.Visibility = Visibility.Visible;
            splitMDCB.IsChecked = metadataResource.SplitMetadata;
            mdPathTB.Text = "Metadata Path: " + metadataResource.MetadataPath;
        }
        else if (resource is FileResource fileResource)
        {
            fileExpander.Visibility = Visibility.Visible;
            fileTypeTB.Text = "File Type: " + fileResource.FileType.ToString();
            filePathTB.Text = "File Path: " + fileResource.FilePath;
            fileDescriptionTB.Text = fileResource.Description;
            _fileAuthors.LoadFromList(fileResource.Authors);
            _fileTags.Clear();
            _fileTags.AddRange(fileResource.Tags);
        }

        if (resource is ProjectFolder)
        {
            idTB.Visibility = Visibility.Collapsed;
            uriTB.Visibility = Visibility.Collapsed;
            genButton.Visibility = Visibility.Collapsed;
            metadataExpander.Visibility = Visibility.Collapsed;
        }

        if (resource is Subject or QuestionBank)
        {
            scCB.Visibility = Visibility.Visible;
            scCB.SelectedItem = resource switch
            {
                Subject sub => sub.SubjectCode,
                QuestionBank qb => qb.SubjectCode,
                _ => SubjectList.None
            };
        }
    }

    public void UpdateResource()
    {
        IResource? resource = Resource;
        if (resource == null) return;

        resource.Title = titleTB.Text.ToNullIfEmpty();
        resource.Id = idTB.Text.ToNullIfEmpty();
        try
        {
            if (Uri.TryCreate(uriTB.Text, UriKind.Absolute, out Uri? uri))
                resource.Uri = uri;
        }
        catch { }

        if (resource is PackageResource package)
        {
            package.Description = packageDescriptionTB.Text.ToNullIfEmpty();
            if (Version.TryParse(packageVersionTB.Text, out Version? version))
            {
                package.Version = version;
            }
            package.Authors = _packageAuthors.UnwrapToList();
            package.Tags = _packageTags.ToList();
        }
        else if (resource is MetadataResource metadataResource)
        {
            metadataResource.SplitMetadata = splitMDCB.IsChecked ?? false;
        }
        else if (resource is FileResource fileResource)
        {
            fileResource.Description = fileDescriptionTB.Text.ToNullIfEmpty();
            fileResource.Authors = _fileAuthors.UnwrapToList();
            fileResource.Tags = _fileTags.ToList();
        }

        if (resource is Subject sub)
            sub.SubjectCode = (SubjectList)scCB.SelectedItem;
        else if (resource is QuestionBank qb)
            qb.SubjectCode = (SubjectList)scCB.SelectedItem;
    }

    private void OpenParentResource() => OpenResource(Resource?.ParentResource);

    private static void OpenResource(IResource? resource) => EditorPagesManager.CreateOrOpenEditor(resource);

    private void ChildButton_Click(object sender, RoutedEventArgs e) => OpenResource((sender as HyperlinkButton)?.DataContext as IResource);

    private void GenButton_Click(object sender, RoutedEventArgs e) => GenerateIdAndUriFromAncestors();

    private void GenerateIdAndUriFromAncestors()
    {
        idTB.Text = GenerateIdFromAncestors(Resource, "Symptum");
        uriTB.Text = GenerateUriFromAncestors(Resource, ResourceManager.DefaultUriScheme);
    }

    private string? ConvertResourceTitleToId(string? title) => RemoveIllegalCharacters(title, ch => ch != ' ');

    private string? ConvertResourceTitleToUri(string? title) => ConvertResourceTitleToId(title)?.ToLowerInvariant();

    private string? GenerateIdFromAncestors(IResource? resource, string? prefix = null)
    {
        string? id = prefix;
        if (resource != null)
            id = (resource.ParentResource?.Id ?? prefix + GenerateIdFromAncestors(resource.ParentResource))
                + "." + ConvertResourceTitleToId(resource.Title);

        return id;
    }

    private string? GenerateUriFromAncestors(IResource? resource, string? prefix = null)
    {
        string? id = prefix;
        if (resource != null)
            id = (resource.ParentResource?.Uri?.ToString() ?? prefix + GenerateUriFromAncestors(resource.ParentResource))
                + (resource.ParentResource != null ? "/" : string.Empty)
                + ConvertResourceTitleToUri(resource.Title);

        return id;
    }

    private void HandleListEditors()
    {
        #region Package Authors

        packageAuthorsLE.ItemsSource = _packageAuthors;
        packageAuthorsLE.AddItemRequested += (s, e) => _packageAuthors.Add(new ListEditorItemWrapper<AuthorInfo>(new()));
        packageAuthorsLE.ClearItemsRequested += (s, e) => _packageAuthors.Clear();
        packageAuthorsLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
                _packageAuthors.Remove(author);
        };
        packageAuthorsLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
                _packageAuthors.Add(new() { Value = new() { Name = author.Value.Name, Email = author.Value.Email } });
        };
        packageAuthorsLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
            {
                int oldIndex = _packageAuthors.IndexOf(author);
                int newIndex = Math.Max(oldIndex - 1, 0);
                _packageAuthors.Move(oldIndex, newIndex);
            }
        };
        packageAuthorsLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
            {
                int oldIndex = _packageAuthors.IndexOf(author);
                int newIndex = Math.Min(oldIndex + 1, _packageAuthors.Count - 1);
                _packageAuthors.Move(oldIndex, newIndex);
            }
        };

        #endregion

        #region File Authors

        fileAuthorsLE.ItemsSource = _fileAuthors;
        fileAuthorsLE.AddItemRequested += (s, e) => _fileAuthors.Add(new ListEditorItemWrapper<AuthorInfo>(new()));
        fileAuthorsLE.ClearItemsRequested += (s, e) => _fileAuthors.Clear();
        fileAuthorsLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
                _fileAuthors.Remove(author);
        };
        fileAuthorsLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
                _fileAuthors.Add(new() { Value = new() { Name = author.Value.Name, Email = author.Value.Email } });
        };
        fileAuthorsLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
            {
                int oldIndex = _fileAuthors.IndexOf(author);
                int newIndex = Math.Max(oldIndex - 1, 0);
                _fileAuthors.Move(oldIndex, newIndex);
            }
        };
        fileAuthorsLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<AuthorInfo> author)
            {
                int oldIndex = _fileAuthors.IndexOf(author);
                int newIndex = Math.Min(oldIndex + 1, _fileAuthors.Count - 1);
                _fileAuthors.Move(oldIndex, newIndex);
            }
        };

        #endregion
    }
}
