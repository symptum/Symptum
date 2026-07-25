using System.Collections.ObjectModel;
using Symptum.Common.ProjectSystem;
using Symptum.Core.Data;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Editor.Pages;
using Symptum.Editor.ViewModels;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Editor.Controls;

public sealed partial class ResourcePropertiesEditorControl : UserControl
{
    private const string h_file = "File";
    private const string h_package = "Package";

    private readonly ObservableCollection<ListEditorItemWrapper<AuthorInfo>> _authors = [];
    private readonly ObservableCollection<string> _tags = [];

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

    public ResourcePropertiesEditorControl()
    {
        InitializeComponent();
    }

    private void ResourcePropertiesEditorControl_Loaded(object? s, RoutedEventArgs e)
    {
        LoadResource(Resource);
        authorsLE.ItemsSource = _authors;
        authorsLE.ActionRequested += LE_ActionRequested;
    }

    private void ResourcePropertiesEditorControl_Unloaded(object? s, RoutedEventArgs e)
    {
        authorsLE.ItemsSource = null;
        _authors.ClearWrapperListSafe();
        authorsLE.ActionRequested -= LE_ActionRequested;
    }

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
        filePkgExpander.Visibility = Visibility.Collapsed;
        filePkgExpander.Header = null;
        descriptionTB.Text = null;
        packageVersionTB.Text = null;
        packageVersionTB.Visibility = Visibility.Collapsed;
        _authors.ClearWrapperListSafe();
        _tags.Clear();
        metadataExpander.Visibility = Visibility.Collapsed;
        splitMDCB.IsChecked = null;
        mdPathTB.Text = null;
        fileTypeTB.Text = null;
        fileTypeTB.Visibility = Visibility.Collapsed;
        filePathTB.Text = null;
        filePathTB.Visibility = Visibility.Collapsed;
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
            filePkgExpander.Visibility = Visibility.Visible;
            filePkgExpander.Header = h_package;
            descriptionTB.Text = package.Description;
            packageVersionTB.Text = package.Version?.ToString();
            packageVersionTB.Visibility = Visibility.Visible;
            _authors.LoadFromList(package.Authors);
            _tags.Clear();
            _tags.AddRange(package.Tags);
        }
        else if (resource is MetadataResource metadataResource)
        {
            metadataExpander.Visibility = Visibility.Visible;
            splitMDCB.IsChecked = metadataResource.SplitMetadata;
            mdPathTB.Text = "Metadata Path: " + metadataResource.MetadataPath;
        }
        else if (resource is FileResource fileResource)
        {
            filePkgExpander.Visibility = Visibility.Visible;
            filePkgExpander.Header = h_file;
            fileTypeTB.Text = "File Type: " + fileResource.FileType.ToString();
            fileTypeTB.Visibility = Visibility.Visible;
            filePathTB.Text = "File Path: " + fileResource.FilePath;
            filePathTB.Visibility = Visibility.Visible;
            descriptionTB.Text = fileResource.Description;
            _authors.LoadFromList(fileResource.Authors);
            _tags.Clear();
            _tags.AddRange(fileResource.Tags);
        }

        if (resource is ProjectFolder)
        {
            idTB.Visibility = Visibility.Collapsed;
            uriTB.Visibility = Visibility.Collapsed;
            genButton.Visibility = Visibility.Collapsed;
            metadataExpander.Visibility = Visibility.Collapsed;
        }

        if (resource is Subject)
        {
            scCB.Visibility = Visibility.Visible;
            scCB.SelectedItem = resource switch
            {
                Subject sub => sub.SubjectCode,
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
            package.Description = descriptionTB.Text.ToNullIfEmpty();
            if (Version.TryParse(packageVersionTB.Text, out Version? version))
            {
                package.Version = version;
            }
            package.Authors = _authors.UnwrapToList();
            package.Tags = [.. _tags];
        }
        else if (resource is MetadataResource metadataResource)
        {
            metadataResource.SplitMetadata = splitMDCB.IsChecked ?? false;
        }
        else if (resource is FileResource fileResource)
        {
            fileResource.Description = descriptionTB.Text.ToNullIfEmpty();
            fileResource.Authors = _authors.UnwrapToList();
            fileResource.Tags = [.. _tags];
        }

        if (resource is Subject sub)
            sub.SubjectCode = (SubjectList)scCB.SelectedItem;
    }

    private void ParentResourceButton_Click(object s, RoutedEventArgs e) => OpenResource(Resource?.ParentResource);

    private static void OpenResource(IResource? resource) => EditorPagesManager.CreateOrOpenEditor(resource);

    private void ChildButton_Click(object sender, RoutedEventArgs e) => OpenResource((sender as HyperlinkButton)?.DataContext as IResource);

    private void GenButton_Click(object sender, RoutedEventArgs e) => GenerateIdAndUriFromAncestors();

    private void GenerateIdAndUriFromAncestors()
    {
        idTB.Text = GenerateIdFromAncestors(Resource, "Symptum");
        uriTB.Text = GenerateUriFromAncestors(Resource, ResourceManager.DefaultUriScheme);
    }

    private string ConvertResourceTitleToId(string? title) => RemoveIllegalCharacters(title, ch => ch != ' ');

    private string ConvertResourceTitleToUri(string? title) => ConvertResourceTitleToId(title).ToLowerInvariant();

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
            id = (resource.ParentResource?.Uri?.ToString().TrimEnd('/') ?? prefix + GenerateUriFromAncestors(resource.ParentResource))
                + (resource.ParentResource != null ? "/" : string.Empty)
                + ConvertResourceTitleToUri(resource.Title);

        return id;
    }

    private void LE_ActionRequested(object? s, ListEditorItemActionRequestedEventArgs e) =>
        ListEditorControl.HandleActionRequired(_authors, e, () => MainViewModel.Instance.CurrentAuthor, a => new() { Email = a.Email, Name = a.Name });
}
