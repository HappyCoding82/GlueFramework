using Dapper;
using GlueFramework.Core.Abstractions;
using System.Data;
using System.Reflection;
using System.Text;
using static GlueFramework.Core.ORM.SqlBuilderFactory;
using QueryLockType = GlueFramework.Core.Abstractions.QueryLockType;

namespace GlueFramework.Core.ORM
{
    public class SqlBuilder_Sqlite<T>:SqlBuilder_Base<T>, ISqlBuilder<T>, ISqlBuilderPartition
        where T : class
    {
        protected override char GetNamePrefix()
        {
            return '[';
        }

        protected override char GetNameSuffix()
        {
            return ']';
        }

        protected override string BuildInsertedIdSql()
        {
            return "(SELECT last_insert_rowid())";
        }

        public override string GetSelectTopRecordsSql(int number, QueryLockType lockType = QueryLockType.None)
        {
            ThrowIfLockRequested(lockType);
            var selectStatement = $"Select {GetFieldList()} FROM {TableNameForSql()} limit {number}; ";
            return selectStatement;
        }

        public override string GetQueryTopRecordsSql(int number, string where, QueryLockType lockType = QueryLockType.None)
        {
            ThrowIfLockRequested(lockType);
            var selectStatement = $"Select {GetFieldList()} FROM {TableNameForSql()} {where} limit {number}; ";
            return selectStatement;
        }

        protected override string GetTableLockHint(QueryLockType lockType)
        {
            ThrowIfLockRequested(lockType);
            return string.Empty;
        }

        protected override string GetSuffixLockClause(QueryLockType lockType)
        {
            ThrowIfLockRequested(lockType);
            return string.Empty;
        }

        private static void ThrowIfLockRequested(QueryLockType lockType)
        {
            if (lockType != QueryLockType.None)
                throw new NotSupportedException($"SQLite does not support row-level lock hints ({lockType}).");
        }

        public SqlBuilder_Sqlite(IDataTablePrefixProvider dataTablePrefixProvider = null)
        {
            Analyze<T>(dataTablePrefixProvider);
        }

        protected override  string GetSelectByPagerSql(string filter, string orderby, int pageIndex, int pageSize, QueryLockType lockType = QueryLockType.None)
        {
            ThrowIfLockRequested(lockType);
            int skip = 1;
            if (pageIndex > 0)
            {
                skip = (pageIndex - 1) * pageSize + 1;
            }
            StringBuilder sb = new StringBuilder();
            var fieldList = GetFieldList();

            sb.AppendFormat(@$"SELECT  {fieldList} FROM(
                               SELECT ROW_NUMBER() OVER({orderby}) AS RowNum,{fieldList} 
                                FROM  {TableNameForSql()} {filter}) AS result
                                WHERE  RowNum >= {skip}   AND RowNum <= {pageIndex * pageSize}
                                {orderby}");
            return sb.ToString();
        }

        public string GetSelectByFilterSql(string filter, int recordNumber)
        {
            return $"Select Top {recordNumber} { GetFieldList() } FROM {TableNameForSql() } WHERE {filter}; ";
        }

        public string GetSelectByFilterSql(string filter, int recordNumber, string orderBy)
        {
            return $"Select Top {recordNumber} { GetFieldList() } FROM {TableNameForSql() } WHERE {filter} Order by {orderBy}; ";
        }

        protected override DBTypes GetDbType()
        {
            return DBTypes.SQLITE;
        }

        protected override string BuildBatchInsertValuePart(List<string> values)
        {
            return string.Join(" UNION ", values.Select(x => " SELECT " + x));
        }

    }
}
