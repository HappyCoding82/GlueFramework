using GlueFramework.Core.Abstractions;
using System;

namespace GlueFramework.Core.ORM
{
    public class DBFieldAttribute : Attribute, IAttributeName
    {
        public string FieldName { get; set; }
        public bool IsKeyField { get; private set; }
        public bool AutoGenerate { get; }
        public string[] Groups { get; set; }

        public DBFieldAttribute()
        { 
        }

        public DBFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

        public DBFieldAttribute(string fieldName, bool isKeyField)
        {
            FieldName = fieldName;
            this.IsKeyField = isKeyField;
        }

        public DBFieldAttribute(string fieldName, bool isKeyField, bool autogenerate)
        {
            FieldName = fieldName;
            this.IsKeyField = isKeyField;
            this.AutoGenerate = autogenerate;
        }

        public string GetName()
        {
            return FieldName;
        }
    }
}
