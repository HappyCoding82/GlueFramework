using Dapper;
using GlueFramework.Core.Abstractions;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using static GlueFramework.Core.ORM.SqlBuilderFactory;

namespace GlueFramework.Core.ORM
{
    public class SqlBuilder_MsSql<T> : SqlBuilder_Base<T>, ISqlBuilder<T>, ISqlBuilderPartition
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
            return "(Select Cast(SCOPE_IDENTITY() as INT))";
        }

        public override string GetSelectTopRecordsSql(int number, QueryLockType lockType = QueryLockType.None)
        {
            var lockHint = lockType == QueryLockType.None ? string.Empty : GetTableLockHint(lockType);
            var selectStatement = $"Select Top {number} {GetFieldList()} FROM {TableNameForSql()}{lockHint} ; ";
            return selectStatement;
        }

        public override string GetQueryTopRecordsSql(int number, string where, QueryLockType lockType = QueryLockType.None)
        {
            var lockHint = lockType == QueryLockType.None ? string.Empty : GetTableLockHint(lockType);
            var selectStatement = $"Select Top {number} {GetFieldList()} FROM {TableNameForSql()}{lockHint} {where} ; ";
            return selectStatement;
        }

        protected override string GetTableLockHint(QueryLockType lockType)
        {
            return lockType switch
            {
                QueryLockType.ForUpdate => " WITH (UPDLOCK, ROWLOCK, HOLDLOCK)",
                QueryLockType.ForShare => " WITH (HOLDLOCK, ROWLOCK)",
                _ => throw new NotSupportedException($"Lock type {lockType} is not supported on SQL Server.")
            };
        }


        public SqlBuilder_MsSql(IDataTablePrefixProvider dataTablePrefixProvider = null)
        {
            Analyze<T>(dataTablePrefixProvider);
        }

        protected override string GetSelectByPagerSql(string filter, string orderby, int pageIndex, int pageSize, QueryLockType lockType = QueryLockType.None)
        {
            int skip = 1;
            if (pageIndex > 0)
            {
                skip = (pageIndex - 1) * pageSize + 1;
            }
            StringBuilder sb = new StringBuilder();
            var fieldList = GetFieldList();
            var lockHint = lockType == QueryLockType.None ? string.Empty : GetTableLockHint(lockType);

            sb.AppendFormat(@$"SELECT  {fieldList} FROM(
                               SELECT ROW_NUMBER() OVER({orderby}) AS RowNum,{fieldList} 
                                FROM  {TableNameForSql()}{lockHint} {filter}) AS result
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
            return  DBTypes.SQLSERVER;
        }

        protected override string BuildBatchInsertValuePart(List<string> values)
        {
            return string.Join(" UNION ", values.Select(x => " SELECT " + x));
        }
       
    }
}
