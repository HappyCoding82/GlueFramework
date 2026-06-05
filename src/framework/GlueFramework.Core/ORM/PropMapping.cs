using System;

namespace GlueFramework.Core.ORM
{
    public class PropMapping
    {
        private string _fieldName = string.Empty;
        public string FieldName
        {
            get
            {
                return string.IsNullOrEmpty(_fieldName) ? PropertyName : _fieldName;
            }
            set
            {
                _fieldName = value;
            }
        }

        public bool IsKey { get; set; }

        public bool AutoGenerate { get; set; }

        public string PropertyName { get; set; }

        public string ParameterName => "@" + PropertyName;

        public Type PropertyType { get; set; }

        public List<string> FieldGroups { get; set; } = new List<string>();
    }
}
