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

        public override string GetSelectTopRecordsSql(int number, QueryLockType lockType = QueryLockType.None)
        {
            var lockClause = lockType == QueryLockType.None ? string.Empty : GetSuffixLockClause(lockType);
            var selectStatement = $"Select {GetFieldList()} FROM {TableNameForSql()} limit {number}{lockClause}; ";
            return selectStatement;
        }

        public override string GetQueryTopRecordsSql(int number, string where, QueryLockType lockType = QueryLockType.None)
        {
            var lockClause = lockType == QueryLockType.None ? string.Empty : GetSuffixLockClause(lockType);
            var selectStatement = $"Select {GetFieldList()} FROM {TableNameForSql()} {where} limit {number}{lockClause}; ";
            return selectStatement;
        }

        public SqlBuilder_MySql(IDataTablePrefixProvider dataTablePrefixProvider = null)
        {
            Analyze<T>(dataTablePrefixProvider);
        }

        protected override string GetSelectByPagerSql(string filter, string orderby, int pageIndex, int pageSize, QueryLockType lockType = QueryLockType.None)
        {
            int skip = 0;
            StringBuilder sb = new StringBuilder();
            var fieldList = GetFieldList();
            var lockClause = lockType == QueryLockType.None ? string.Empty : GetSuffixLockClause(lockType);

            if (pageIndex > 1)
            {
                skip = (pageIndex - 1) * pageSize;
                sb.AppendFormat(@$"SELECT  {fieldList} FROM {TableNameForSql()} {filter} {orderby} limit {skip},{pageSize}{lockClause}");
            }
            else
                sb.AppendFormat(@$"SELECT  {fieldList} FROM {TableNameForSql()} {filter} {orderby} limit {pageSize}{lockClause}");

            return sb.ToString();
        }

        protected override string GetSuffixLockClause(QueryLockType lockType)
        {
            return lockType switch
            {
                QueryLockType.ForUpdate => " FOR UPDATE",
                QueryLockType.ForShare => " LOCK IN SHARE MODE",
                _ => throw new NotSupportedException($"Lock type {lockType} is not supported on MySQL.")
            };
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
