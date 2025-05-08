using System.Collections;

namespace Arcor2.ClientSdk.ClientServices.ConsoleTestApp.Output;

public static class ReflectionHelper {
    // ANSI color codes
    private const string ResetColor = "\x1b[0m";

    private static readonly string[] BraceColors = {
        "\x1b[38;5;11m", // Yellow
        "\x1b[38;5;10m", // Lime
        "\x1b[38;5;14m", // Cyan
        "\x1b[38;5;13m", // Magenta
        "\x1b[38;5;12m" // Blue
    };

    /// <summary>
    ///     Recursively pretty-prints the object properties.
    /// </summary>
    /// <param name="obj">The object to be printed.</param>
    /// <param name="indentLevel">The base level of indentation.</param>
    /// <param name="objectName">Custom object name to be prefixed.</param>
    /// <returns></returns>
    public static string FormatObjectProperties(object? obj, int indentLevel = 0, string? objectName = null) {
        if(indentLevel > 3) {
            return "HIDDEN";
        }
        if(obj == null) {
            return "[null]";
        }

        var typeName = obj.GetType().Name;

        var properties = obj.GetType().GetProperties()
            .Where(property => property.GetIndexParameters().Length == 0) // Skip indexed properties
            .ToList();

        var braceColor = BraceColors[indentLevel % BraceColors.Length];

        var propertyValues = properties
            .Select(property => {
                var value = property.GetValue(obj, null);
                var formattedValue = FormatValue(value, indentLevel);
                return
                    $"{new string(' ', (indentLevel + 1) * 2)}{braceColor}{property.Name}{ResetColor}: {formattedValue}";
            })
            .ToList();

        var prefix = objectName == null ? typeName : $"{objectName}: {typeName}";

        return prefix +
               $" {braceColor}{{{ResetColor}\n{string.Join("\n", propertyValues)}\n{new string(' ', indentLevel * 2)}{braceColor}}}{ResetColor}";
    }

    private static string FormatValue(object? value, int indentLevel) {
        if(value == null) {
            return "null";
        }

        if(value is string) {
            return $"\"{value}\"";
        }

        if(value is IEnumerable enumerable && !(value is string)) {
            var items = enumerable.Cast<object>()
                .Select(item => {
                    if(item == null) {
                        return "null";
                    }

                    if(item.GetType().IsPrimitive || item is string) {
                        return item.ToString();
                    }

                    return FormatObjectProperties(item, indentLevel + 2);
                })
                .ToList();

            var braceColor = BraceColors[(indentLevel + 1) % BraceColors.Length];

            if(items.Count == 0) {
                return $"{braceColor}[]{ResetColor}";
            }

            var indentOne = new string(' ', (indentLevel + 1) * 2);
            var indentTwo = new string(' ', (indentLevel + 2) * 2);

            return
                $"{braceColor}[\n{indentTwo}{ResetColor}{string.Join(", ", items)}{braceColor}\n{indentOne}]{ResetColor}";
        }

        if(value.GetType().IsClass && value.GetType() != typeof(object)) {
            return FormatObjectProperties(value, indentLevel + 1);
        }

        return value.ToString() ?? "null (after string format)";
    }
}