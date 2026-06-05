using GlueFramework.Core.ORM;

namespace GlueFramework.CoreTests.Sql
{
    [DataTable("DemoProduct")]
    internal sealed class DemoProduct
    {
        [DBField("Id", isKeyField: true)]
        public int Id { get; set; }

        [DBField("Name")]
        public string Name { get; set; } = string.Empty;

        [DBField("CategoryId")]
        public int CategoryId { get; set; }

        [DBField("BrandId")]
        public int BrandId { get; set; }

        [DBField("Price")]
        public decimal Price { get; set; }

        [DBField("Qty")]
        public decimal Qty { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
    }

    [DataTable("Category")]
    internal sealed class Category
    {
        [DBField("Id", isKeyField: true)]
        public int Id { get; set; }

        [DBField("CategoryName")]
        public string CategoryName { get; set; } = string.Empty;
    }

    [DataTable("Brand")]
    internal sealed class Brand
    {
        [DBField("Id", isKeyField: true)]
        public int Id { get; set; }

        [DBField("BrandName")]
        public string BrandName { get; set; } = string.Empty;
    }

    internal sealed class DemoProductReportRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
    }

    internal sealed class DemoProductAggregateRow
    {
        public int CategoryId { get; set; }
        public decimal Total { get; set; }
    }

    internal sealed class DemoProductCalcRow
    {
        public decimal AA { get; set; }
        public decimal Qty { get; set; }
    }

    [DataTable("DemoProductIdentity")]
    internal sealed class DemoProductIdentity
    {
        [DBField("Id", isKeyField: true, autogenerate: true)]
        public int Id { get; set; }

        [DBField("Name")]
        public string Name { get; set; } = string.Empty;
    }
}
