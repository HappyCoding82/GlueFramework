using System.Collections.Generic;

namespace GlueFramework.Core.ORM
{
    public class TableMapping
    {
        private string _tableName = string.Empty;


        public string TableName
        {
            get
            {
                return  (string.IsNullOrEmpty(_tableName) ? ClassName : _tableName) ;
            }
            set
            {
                _tableName = value;
            }
        }

        public string ClassName { get; set; }

        private List<PropMapping> _propMappings = new List<PropMapping>();
        public List<PropMapping> PropMappings => _propMappings;
    }
}
