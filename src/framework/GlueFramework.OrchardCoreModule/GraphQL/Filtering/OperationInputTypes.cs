using HotChocolate.Types;

namespace GlueFramework.OrchardCoreModule.GraphQL.Filtering
{
    public class StringOperationFilterInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("StringOperationFilterInput");
            descriptor.Field("eq").Type<StringType>();
            descriptor.Field("contains").Type<StringType>();
            descriptor.Field("startsWith").Type<StringType>();
            descriptor.Field("endsWith").Type<StringType>();
        }
    }

    public class IntOperationFilterInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("IntOperationFilterInput");
            descriptor.Field("eq").Type<IntType>();
            descriptor.Field("gt").Type<IntType>();
            descriptor.Field("gte").Type<IntType>();
            descriptor.Field("lt").Type<IntType>();
            descriptor.Field("lte").Type<IntType>();
            descriptor.Field("in").Type<ListType<NonNullType<IntType>>>();
        }
    }

    public class LongOperationFilterInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("LongOperationFilterInput");
            descriptor.Field("eq").Type<LongType>();
            descriptor.Field("gt").Type<LongType>();
            descriptor.Field("gte").Type<LongType>();
            descriptor.Field("lt").Type<LongType>();
            descriptor.Field("lte").Type<LongType>();
            descriptor.Field("in").Type<ListType<NonNullType<LongType>>>();
        }
    }

    public class DecimalOperationFilterInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("DecimalOperationFilterInput");
            descriptor.Field("eq").Type<DecimalType>();
            descriptor.Field("gt").Type<DecimalType>();
            descriptor.Field("gte").Type<DecimalType>();
            descriptor.Field("lt").Type<DecimalType>();
            descriptor.Field("lte").Type<DecimalType>();
            descriptor.Field("in").Type<ListType<NonNullType<DecimalType>>>();
        }
    }

    public class DateTimeOperationFilterInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("DateTimeOperationFilterInput");
            descriptor.Field("eq").Type<DateTimeType>();
            descriptor.Field("gt").Type<DateTimeType>();
            descriptor.Field("gte").Type<DateTimeType>();
            descriptor.Field("lt").Type<DateTimeType>();
            descriptor.Field("lte").Type<DateTimeType>();
        }
    }

    public class BoolOperationFilterInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("BoolOperationFilterInput");
            descriptor.Field("eq").Type<BooleanType>();
        }
    }
}
