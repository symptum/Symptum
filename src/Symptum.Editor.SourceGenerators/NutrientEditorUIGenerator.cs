using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Symptum.Editor.SourceGenerators;

/// <summary>
/// A source generator for Symptum.Editor.Controls.FoodEditorDialog
/// </summary>
[Generator]
public class NutrientEditorUIGenerator : ISourceGenerator
{
    private INamedTypeSymbol? attributeSymbol;

    public void Execute(GeneratorExecutionContext context)
    {
        attributeSymbol = context.Compilation.GetTypeByMetadataName("Symptum.Core.CodeAnalysis.GenerateUIAttribute");
        INamedTypeSymbol? classSymbol = context.Compilation.GetTypeByMetadataName("Symptum.Core.Data.Nutrition.Food");

        if (classSymbol == null || attributeSymbol == null) return;

        List<IPropertySymbol> props = [];
        foreach (var prop in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (prop.GetAttributes().Any(x => x.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
            {
                props.Add(prop);
            }
        }

        string classSource = GenerateClass(props);
        context.AddSource($"FoodEditorDialog_UI.g.cs", classSource);
    }

    private string GenerateClass(IEnumerable<IPropertySymbol> props)
    {
        StringBuilder source = new(@"// <auto-generated/>

using Microsoft.UI.Xaml.Controls;

namespace Symptum.Editor.Controls
{
    public sealed partial class FoodEditorDialog
    {");
        GenerateControls(source, props);
        source.Append(@"    }
}
");
        return source.ToString();
    }

    private void GenerateControls(StringBuilder source, IEnumerable<IPropertySymbol> props)
    {
        StringBuilder addControls = new();
        StringBuilder loadNutrients = new();
        StringBuilder clearNutrients = new();
        StringBuilder updateNutrients = new();
        foreach (var prop in props)
        {
            string propName = prop.Name;
            string controlName = GetControlName(propName);
            string? header = null;
            string? description = null;

            if (prop.GetAttributes().SingleOrDefault(x => x.AttributeClass.Equals(attributeSymbol)) is AttributeData data)
            {
                header = data.NamedArguments.FirstOrDefault(x => x.Key == "Header").Value.Value?.ToString();
                description = data.NamedArguments.FirstOrDefault(x => x.Key == "Description").Value.Value?.ToString();
            }

            string desc = !string.IsNullOrEmpty(description) ? $@", Description = ""{description}""" : string.Empty;

            // Creating fields
            source.Append($@"
        private readonly TextBox {controlName} = new() {{ Header = ""{header}""{desc} }};");

            // We are doing the other gens in a single loop;
            addControls.Append($@"
            nutrientsList.Children.Add({controlName});");

            loadNutrients.Append($@"
            {controlName}.Text = Food.{propName};");

            updateNutrients.Append($@"
            Food.{propName} = {controlName}.Text;");

            clearNutrients.Append($@"
            {controlName}.Text = string.Empty;");
        }

        // Adding the generated controls to a StackPanel called nutrientsList
        source.AppendLine();
        source.Append(@"
        private void AddControls()
        {");
        source.Append(addControls.ToString());
        source.AppendLine(@"
        }");

        // Loading the nutrients
        source.Append(@"
        private void LoadNutrients()
        {");
        source.Append(loadNutrients.ToString());
        source.AppendLine(@"
        }");

        // Updating the nutrients
        source.Append(@"
        private void UpdateNutrients()
        {");
        source.Append(updateNutrients.ToString());
        source.AppendLine(@"
        }");

        // Clearing the nutrients
        source.Append(@"
        private void ClearNutrients()
        {");
        source.Append(clearNutrients.ToString());
        source.AppendLine(@"
        }");
    }

    private string GetControlName(string propName)
    {
        if (!string.IsNullOrWhiteSpace(propName))
        {
            string name = propName.Trim();
            name = char.IsLetter(name[0]) ? char.ToLower(name[0]) + name.Substring(1) : name;
            name += "TB";
            return name;
        }

        return string.Empty;
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
