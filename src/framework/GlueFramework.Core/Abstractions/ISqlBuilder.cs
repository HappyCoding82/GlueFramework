using Dapper;
using GlueFramework.Core.LambdaToSQL.Extensions;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace GlueFramework.Core.Abstractions
{
    public interface ISqlBuilder<Model>
    {
        string GetInsertSql();

        string GetDeleteSqlByWhereClause(string filter);

        string GetInsertAndReturnSql();

        string GetSelectTopRecordsSql(int number);
        string GetSelectTopRecordsSql(int number, QueryLockType lockType);

        string GetQueryTopRecordsSql(int number, string where);
        string GetQueryTopRecordsSql(int number, string where, QueryLockType lockType);

        string GetSelectAllSql();
        string GetSelectAllSql(QueryLockType lockType);

        string GetCountSql(string filter);

        string GetSelectByFilterSql(string filter, int recordNumber);

        string GetSelectByFilterSql(string filter, int recordNumber, string orderBy);

        string GetSelectByKeySql();
        string GetSelectByKeySql(QueryLockType lockType);

        string GetUpdateSql();

        string GetUpdateAndReturnSql();

        string GetDeleteByKey();

        string GetCreateSql();

        //string GetBatchInsertSql();
        WherePart GetWherePart(Expression<Func<Model, bool>> expression);
        KeyValuePair<string, DynamicParameters> BuildQuery(Expression<Func<Model, bool>> expression);
        KeyValuePair<string, DynamicParameters> BuildQuery(Expression<Func<Model, bool>> expression, QueryLockType lockType);
        KeyValuePair<string, DynamicParameters> BuildQuery(PagedFilterOptions<Model> filterOpt);
        KeyValuePair<string, DynamicParameters> BuildQueryTop(Expression<Func<Model, bool>> expression, int number);
        KeyValuePair<string, DynamicParameters> BuildQueryTop(Expression<Func<Model, bool>> expression, int number, QueryLockType lockType);
        KeyValuePair<string, DynamicParameters> BuildDeleteSql(Expression<Func<Model, bool>> expression);
        KeyValuePair<string, DynamicParameters> BuildPartialUpdateSql(Model keyModel, IReadOnlyDictionary<string, object?> changes);
        KeyValuePair<string, DynamicParameters> BuildPartialInsertSql(IReadOnlyDictionary<string, object?> changes);
        KeyValuePair<string, DynamicParameters> BuildBatchInsertSql(List<Model> models);
    }
}
