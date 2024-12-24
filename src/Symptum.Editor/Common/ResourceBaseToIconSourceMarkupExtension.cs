using Symptum.Common.ProjectSystem;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;

namespace Symptum.Editor.Common;

internal class ResourceBaseToIconSourceExtension
{
    #region Resource

    public static readonly DependencyProperty ResourceProperty = DependencyProperty.RegisterAttached(
        "Resource",
        typeof(ResourceBase),
        typeof(IconSourceElement),
        new PropertyMetadata(null, OnResourcePropertyChanged));

    public static ResourceBase GetResource(IconSourceElement obj) => (ResourceBase)obj.GetValue(ResourceProperty);

    public static void SetResource(IconSourceElement obj, ResourceBase value) => obj.SetValue(ResourceProperty, value);

    private static void OnResourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconSourceElement iconSourceElement)
        {
            if (e.NewValue is ResourceBase resource)
            {
                iconSourceElement.IconSource = resource switch
                {
                    ProjectFolder => DefaultIconSources.FolderIconSource,
                    Subject => DefaultIconSources.DictionaryIconSource,
                    CsvFileResource => DefaultIconSources.DataGridIconSource,
                    ImageFileResource => DefaultIconSources.PhotoIconSource,
                    MarkdownFileResource => DefaultIconSources.DocumentIconSource,
                    ImageCategoryResource => DefaultIconSources.PicturesIconSource,
                    PackageResource => DefaultIconSources.PackageIconSource,
                    _ => DefaultIconSources.GroupListIconSource
                };
            }
            else
                iconSourceElement.IconSource = null;
        }
    }

    #endregion

    #region ResourceType

    public static readonly DependencyProperty ResourceTypeProperty = DependencyProperty.RegisterAttached(
        "ResourceType",
        typeof(Type),
        typeof(IconSourceElement),
        new PropertyMetadata(null, OnResourceTypePropertyChanged));

    public static Type GetResourceType(IconSourceElement obj) => (Type)obj.GetValue(ResourceTypeProperty);

    public static void SetResourceType(IconSourceElement obj, Type value) => obj.SetValue(ResourceTypeProperty, value);

    private static void OnResourceTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconSourceElement iconSourceElement)
        {
            if (e.NewValue is Type resourceType)
            {
                iconSourceElement.IconSource = typeof(ProjectFolder).IsAssignableFrom(resourceType) ? DefaultIconSources.FolderIconSource :
                                               typeof(Subject).IsAssignableFrom(resourceType) ? DefaultIconSources.DictionaryIconSource :
                                               typeof(CsvFileResource).IsAssignableFrom(resourceType) ? DefaultIconSources.DataGridIconSource :
                                               typeof(ImageFileResource).IsAssignableFrom(resourceType) ? DefaultIconSources.PhotoIconSource :
                                               typeof(MarkdownFileResource).IsAssignableFrom(resourceType) ? DefaultIconSources.DocumentIconSource :
                                               typeof(ImageCategoryResource).IsAssignableFrom(resourceType) ? DefaultIconSources.PicturesIconSource :
                                               typeof(PackageResource).IsAssignableFrom(resourceType) ? DefaultIconSources.PackageIconSource :
                                               DefaultIconSources.GroupListIconSource;
            }
            else
                iconSourceElement.IconSource = null;
        }
    }

    #endregion
}
