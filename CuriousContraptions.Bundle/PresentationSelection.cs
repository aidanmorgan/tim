using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

/// <summary>One property writer for every value of a bounded BuildOnly choice.</summary>
internal static class PresentationSelection
{
    public static void Validate(ElementPresentation presentation, IReadOnlyList<ParameterDescriptor> parameters)
    {
        foreach (var group in presentation.Bindings.GroupBy(binding => (binding.Node, binding.Property)))
        {
            var bindings = group.ToArray();
            if (bindings.All(binding => binding.Selection is null))
            {
                if (bindings.Length != 1) throw new ArgumentException("duplicate property writer");
                continue;
            }
            if (bindings.Any(binding => binding.Selection is null))
                throw new ArgumentException("Unconditional and selected presentation writers overlap.");
            var selected = bindings[0].Selection!.Value;
            var matches = parameters.Where(value => value.Key == selected.Parameter.Value).ToArray();
            if (matches.Length != 1 || matches[0].Kind != ParameterKind.Choice || matches[0].RunEdit != ParameterRunEdit.BuildOnly)
                throw new ArgumentException("Presentation selection requires a declared BuildOnly choice.");
            var parameter = matches[0];
            var options = new HashSet<int>();
            foreach (var binding in bindings)
            {
                var choice = binding.Selection!.Value;
                if (choice.Parameter != selected.Parameter || choice.Option < 0 || choice.Option >= parameter.Options.Count ||
                    !options.Add(choice.Option))
                    throw new ArgumentException("Presentation property writers overlap or name incompatible choices.");
            }
            if (options.Count != parameter.Options.Count)
                throw new ArgumentException("Presentation property is missing a choice writer.");
        }
        foreach (var parameter in presentation.Parameters)
            if (presentation.Bindings.Any(binding => binding.Node == parameter.Node && binding.Property == parameter.Property))
                throw new ArgumentException("Parameter and committed presentation writers overlap.");
    }
}
