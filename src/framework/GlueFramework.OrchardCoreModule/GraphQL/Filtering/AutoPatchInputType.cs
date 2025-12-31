using HotChocolate.Types;
using System;
using System.Collections;
using System.Reflection;

namespace GlueFramework.OrchardCoreModule.GraphQL.Filtering
{
    public class AutoPatchInputType<T> : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name(typeof(T).Name + "PatchInput");

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.GetMethod == null)
                    continue;

                if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fieldName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                var propType = prop.PropertyType;
                var underlying = Nullable.GetUnderlyingType(propType) ?? propType;

                if (underlying == typeof(string))
                {
                    descriptor.Field(fieldName).Type<StringType>();
                    continue;
                }

                if (underlying == typeof(int))
                {
                    descriptor.Field(fieldName).Type<IntType>();
                    continue;
                }

                if (underlying == typeof(long))
                {
                    descriptor.Field(fieldName).Type<LongType>();
                    continue;
                }

                if (underlying == typeof(decimal))
                {
                    descriptor.Field(fieldName).Type<DecimalType>();
                    continue;
                }

                if (underlying == typeof(DateTime))
                {
                    descriptor.Field(fieldName).Type<DateTimeType>();
                    continue;
                }

                if (underlying == typeof(bool))
                {
                    descriptor.Field(fieldName).Type<BooleanType>();
                    continue;
                }

                if (typeof(IEnumerable).IsAssignableFrom(underlying) && underlying != typeof(string))
                {
                    continue;
                }
            }
        }
    }
}
