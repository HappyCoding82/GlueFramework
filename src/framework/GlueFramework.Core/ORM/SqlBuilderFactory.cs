using GlueFramework.Core.Abstractions;

namespace GlueFramework.Core.ORM
{
    public class SqlBuilderFactory
    {
        public enum DBTypes
        { 
            None,
            SQLITE,
            SQLSERVER,
            MYSQL,
            POSTGRESQL
        }

        public static ISqlBuilder<T> CreateInstance<T>(DBTypes dbType, 
            IDataTablePrefixProvider dataTablePrefixProvider = null
            ) where T:class 
        {
            switch (dbType)
            {
                case DBTypes.SQLITE:
                    return new SqlBuilder_Sqlite<T>(dataTablePrefixProvider);
                case DBTypes.SQLSERVER:
                    return new SqlBuilder_MsSql<T>(dataTablePrefixProvider);
                case DBTypes.MYSQL:
                    return new SqlBuilder_MySql<T>(dataTablePrefixProvider);
                case DBTypes.POSTGRESQL:
                    return new SqlBuilder_PostgreSql<T>(dataTablePrefixProvider);
                default:
                    return null;
            }
        }


        public static ISqlBuilderPartition CreatePartitionInstance<T>(DBTypes dbType, 
            IDataTablePrefixProvider dataTablePrefixProvider = null) where T : class
        {
            switch (dbType)
            {
                case DBTypes.SQLITE:
                    return new SqlBuilder_Sqlite<T>(dataTablePrefixProvider);
                case DBTypes.SQLSERVER:
                    return new SqlBuilder_MsSql<T>(dataTablePrefixProvider);
                case DBTypes.MYSQL:
                    return new SqlBuilder_MySql<T>(dataTablePrefixProvider);
                case DBTypes.POSTGRESQL:
                    return new SqlBuilder_PostgreSql<T>(dataTablePrefixProvider);
                default:
                    return null;
            }
        }
    }
}
