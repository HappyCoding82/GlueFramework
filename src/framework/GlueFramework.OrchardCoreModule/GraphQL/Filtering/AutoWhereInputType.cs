using HotChocolate.Types;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace GlueFramework.OrchardCoreModule.GraphQL.Filtering
{
    public class AutoWhereInputType<T> : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name(typeof(T).Name + "WhereInput");

            descriptor.Field("and")
                .Type<ListType<NonNullType<AutoWhereInputType<T>>>>();

            descriptor.Field("or")
                .Type<ListType<NonNullType<AutoWhereInputType<T>>>>();

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.GetMethod == null)
                    continue;

                var fieldName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

                var propType = prop.PropertyType;
                var underlying = Nullable.GetUnderlyingType(propType) ?? propType;

                if (underlying == typeof(string))
                {
                    descriptor.Field(fieldName).Type<Filtering.StringOperationFilterInputType>();
                    continue;
                }

                if (underlying == typeof(int))
                {
                    descriptor.Field(fieldName).Type<Filtering.IntOperationFilterInputType>();
                    continue;
                }

                if (underlying == typeof(long))
                {
                    descriptor.Field(fieldName).Type<Filtering.LongOperationFilterInputType>();
                    continue;
                }

                if (underlying == typeof(decimal))
                {
                    descriptor.Field(fieldName).Type<Filtering.DecimalOperationFilterInputType>();
                    continue;
                }

                if (underlying == typeof(DateTime))
                {
                    descriptor.Field(fieldName).Type<Filtering.DateTimeOperationFilterInputType>();
                    continue;
                }

                if (underlying == typeof(bool))
                {
                    descriptor.Field(fieldName).Type<Filtering.BoolOperationFilterInputType>();
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
