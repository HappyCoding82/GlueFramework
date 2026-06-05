using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.LambdaToSQL.Extensions;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static GlueFramework.Core.ORM.SqlBuilderFactory;

namespace GlueFramework.Core.ORM
{

    public class SqlBuilder_MySql<T> : SqlBuilder_Base<T>, ISqlBuilder<T>, ISqlBuilderPartition
        where T : class
    {

        protected override char GetNamePrefix()
        {
            return '`';
        }

        protected override char GetNameSuffix()
        {
            return '`';
        }


        protected override string BuildInsertedIdSql()
        {
            return "(select @@IDENTITY)";
        }

        public override string GetSelectTopRecordsSql(int number)
        {
            var selectStatement = $"Select {GetFieldList()} FROM {TableNameForSql()} limit {number}; ";
            return selectStatement;
        }

        public override string GetQueryTopRecordsSql(int number, string where)
        {
            var selectStatement = $"Select {GetFieldList()} FROM {TableNameForSql()} {where} limit {number}; ";
            return selectStatement;
        }

        public SqlBuilder_MySql(IDataTablePrefixProvider dataTablePrefixProvider = null)
        {
            Analyze<T>(dataTablePrefixProvider);
        }

        protected override string GetSelectByPagerSql(string filter, string orderby, int pageIndex, int pageSize)
        {
            int skip = 0;
            StringBuilder sb = new StringBuilder();
            var fieldList = GetFieldList();

            if (pageIndex > 1)
            {
                skip = (pageIndex - 1) * pageSize;
                sb.AppendFormat(@$"SELECT  {fieldList} FROM {TableNameForSql()} {filter} {orderby} limit {skip},{pageSize}");
            }
            else
                sb.AppendFormat(@$"SELECT  {fieldList} FROM {TableNameForSql()} {filter} {orderby} limit {pageSize}");

            return sb.ToString();
        }


        public string GetSelectByFilterSql(string filter, int recordNumber)
        {
            return $"Select {GetFieldList()} FROM {TableNameForSql()} WHERE {filter} limit {recordNumber}; ";
        }

        public string GetSelectByFilterSql(string filter, int recordNumber, string orderBy)
        {
            return $"Select {GetFieldList()} FROM {TableNameForSql()} WHERE {filter} Order by {orderBy} limit {recordNumber}; ";
        }

        protected override DBTypes GetDbType()
        {
            return DBTypes.MYSQL;
        }


    }
}
