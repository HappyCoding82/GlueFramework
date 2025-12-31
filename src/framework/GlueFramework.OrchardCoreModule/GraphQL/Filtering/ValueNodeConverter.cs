using HotChocolate.Language;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GlueFramework.OrchardCoreModule.GraphQL.Filtering
{
    public static class ValueNodeConverter
    {
        public static Dictionary<string, object?> ToDictionary(IValueNode node)
        {
            if (node == null || node is NullValueNode)
                throw new ArgumentException("Expected a non-null object value node.", nameof(node));

            if (node is not ObjectValueNode obj)
                throw new ArgumentException("Expected an object value node.", nameof(node));

            return ToDictionary(obj);
        }

        public static IReadOnlyDictionary<string, object?> ToPascalCaseDictionary(IValueNode node)
        {
            var dict = ToDictionary(node);
            var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in dict)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;

                var key = char.ToUpperInvariant(kv.Key[0]) + kv.Key.Substring(1);
                normalized[key] = kv.Value;
            }

            return normalized;
        }

        public static Dictionary<string, object?>? ToDictionaryOrNull(IValueNode? node)
        {
            if (node == null || node is NullValueNode)
                return null;

            if (node is not ObjectValueNode obj)
                throw new ArgumentException("Expected an object value node.", nameof(node));

            return ToDictionary(obj);
        }

        private static Dictionary<string, object?> ToDictionary(ObjectValueNode obj)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in obj.Fields)
            {
                dict[field.Name.Value] = ToClrValue(field.Value);
            }
            return dict;
        }

        private static object? ToClrValue(IValueNode node)
        {
            return node switch
            {
                NullValueNode => null,
                ObjectValueNode o => ToDictionary(o),
                ListValueNode l => ToList(l),
                IntValueNode i => i.ToInt32(),
                FloatValueNode f => ParseDecimal(f.Value),
                StringValueNode s => s.Value,
                BooleanValueNode b => b.Value,
                EnumValueNode e => e.Value,
                _ => node.Value
            };
        }

        private static List<object?> ToList(ListValueNode list)
        {
            var rs = new List<object?>(list.Items.Count);
            foreach (var item in list.Items)
            {
                rs.Add(ToClrValue(item));
            }
            return rs;
        }

        private static decimal ParseDecimal(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return 0m;
        }
    }
}
