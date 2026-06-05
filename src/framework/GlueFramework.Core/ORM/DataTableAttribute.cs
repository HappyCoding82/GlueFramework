using GlueFramework.Core.Abstractions;
using System;

namespace GlueFramework.Core.ORM
{
    public class DataTableAttribute : Attribute, IAttributeName
    {
        public DataTableAttribute(string tableName)
        {
            this.TableName = tableName;
        }

        public string TableName { get; }

        public string GetName()
        {
            return TableName;
        }
    }
}
