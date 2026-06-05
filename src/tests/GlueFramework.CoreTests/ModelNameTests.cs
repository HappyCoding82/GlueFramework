using GlueFramework.Core.ORM;
using System.Linq.Expressions;

namespace GlueFramework.CoreTests
{
    [TestClass]
    public sealed class ModelNameTests
    {
        [DataTable("t_demo")]
        private sealed class DemoWithAttr
        {
            [DBField("f_id")]
            public int Id { get; set; }

            public string? Name { get; set; }
        }

        private sealed class DemoWithoutAttr
        {
            public int Id { get; set; }
        }

        [TestMethod]
        public void Table_WhenDataTableAttributePresent_UsesAttributeValue()
        {
            Assert.AreEqual("t_demo", ModelName.Table<DemoWithAttr>());
        }

        [TestMethod]
        public void Table_WhenDataTableAttributeMissing_UsesTypeName()
        {
            Assert.AreEqual(nameof(DemoWithoutAttr), ModelName.Table<DemoWithoutAttr>());
        }

        [TestMethod]
        public void Column_WhenDBFieldAttributePresent_UsesAttributeValue()
        {
            Assert.AreEqual("f_id", ModelName.Column<DemoWithAttr>(x => x.Id));
        }

        [TestMethod]
        public void Column_WhenDBFieldAttributeMissing_UsesPropertyName()
        {
            Assert.AreEqual(nameof(DemoWithAttr.Name), ModelName.Column<DemoWithAttr>(x => x.Name));
        }

        [TestMethod]
        public void Column_WhenUsingStringPropertyName_ResolvesAttributeOrFallsBack()
        {
            Assert.AreEqual("f_id", ModelName.Column<DemoWithAttr>(nameof(DemoWithAttr.Id)));
            Assert.AreEqual(nameof(DemoWithAttr.Name), ModelName.Column<DemoWithAttr>(nameof(DemoWithAttr.Name)));
        }

        [TestMethod]
        public void Column_WhenExpressionIsNull_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => ModelName.Column<DemoWithAttr>((Expression<Func<DemoWithAttr, object?>>)null!));
        }

        [TestMethod]
        public void Column_WhenPropertyNameIsBlank_Throws()
        {
            Assert.ThrowsException<ArgumentException>(() => ModelName.Column<DemoWithAttr>(""));
        }
    }
}
