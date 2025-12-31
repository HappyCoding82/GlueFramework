using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.LambdaToSQL.Extensions;
using GlueFramework.Core.ORM.LambdaToSQL;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static GlueFramework.Core.ORM.SqlBuilderFactory;

namespace GlueFramework.Core.ORM
{
    public abstract class SqlBuilder_Base<T> where T : class
    {
       
        protected string _typeName = "";
        protected TableMapping _tbMapping = null;
        protected string _tablePrefix = string.Empty;
        protected string _schema = string.Empty;
        protected virtual void Analyze<T>(IDataTablePrefixProvider dataTablePrefixProvider = null)
        {
            Type type = typeof(T);
            _typeName = type.FullName;
            _tbMapping = DBModelAnalysisContext.Mappings.GetOrAdd(_typeName, _ =>
            {
                TableMapping tbMapping = new TableMapping();
                var tableAttr = type.GetCustomAttribute<DataTableAttribute>();
                tbMapping.TableName = tableAttr == null ? type.Name : tableAttr.TableName;

                var properties = type.GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.CustomAttributes.Any(x => x.AttributeType == typeof(DBFieldNotMappedAttribute)) ||
                         prop.PropertyType.Name.StartsWith("List"))
                    {
                        continue;
                    }
                    var propMapping = new PropMapping() { PropertyName = prop.Name, PropertyType = prop.PropertyType };
                    var fieldInfo = prop.GetCustomAttribute<DBFieldAttribute>(true);
                    if (fieldInfo != null)
                    {
                        propMapping.IsKey = fieldInfo.IsKeyField;
                        propMapping.FieldName = fieldInfo.FieldName;
                        propMapping.AutoGenerate = fieldInfo.AutoGenerate;
                        if (fieldInfo.Groups != null)
                        {
                            propMapping.FieldGroups = fieldInfo.Groups.ToList();
                        }
                    }
                    tbMapping.PropMappings.Add(propMapping);
                }
                return tbMapping;
            });

            if (dataTablePrefixProvider != null)
            {
                _tablePrefix = dataTablePrefixProvider.Prefix;

                // Schema support (OrchardCore / YesSql tenants): keep ctor signature as
                // IDataTablePrefixProvider for backward compatibility, but if the provider
                // also exposes Schema we compose a schema-qualified prefix.
                if (dataTablePrefixProvider is ITenantTableSettingsProvider ts &&
                    !string.IsNullOrWhiteSpace(ts.Schema))
                {
                    _schema = ts.Schema;
                }
                
            }
        }

        protected abstract char GetNamePrefix();
        protected abstract char GetNameSuffix();
        protected virtual string PopulateName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return GetNamePrefix().ToString() + GetNameSuffix().ToString();

            if (rawName.Contains('.'))
                throw new ArgumentException($"'{nameof(rawName)}' must not contain '.' because schema qualification is handled by {nameof(TableNameForSql)}.", nameof(rawName));

            return GetNamePrefix() + rawName + GetNameSuffix();
        }
        protected abstract string GetSelectByPagerSql(string filter, string orderby, int pageIndex, int pageSize);
        public abstract string GetSelectTopRecordsSql(int number);
        public abstract string GetQueryTopRecordsSql(int number, string where);

        protected virtual string TableNameForSql()
        {
            var name = _tablePrefix + _tbMapping.TableName;
            if (!string.IsNullOrWhiteSpace(_schema))
                return PopulateName(_schema) + "." + PopulateName(name);
            else
                return PopulateName(name);
        }

        protected virtual string TableNameForSql<Model>(Model m) where Model : PartitionModelBase
        {
            var name = _tablePrefix + _tbMapping.TableName + m.GetPartition();
            if (!string.IsNullOrWhiteSpace(_schema))
                return PopulateName(_schema) + "." + PopulateName(name);
            else
                return PopulateName(name);
        }

        public virtual string GetInsertSql()
        {

                string[] strSqlNames = _tbMapping.PropMappings.Where(x => x.AutoGenerate == false).Select(p => $"{PopulateName( p.FieldName ) }").ToArray();
                string strSqlName = string.Join(",", strSqlNames);
                string[] strSqlValues = _tbMapping.PropMappings.Where(x => x.AutoGenerate == false).Select(P => $"{P.ParameterName}").ToArray();
                string strSqlValue = string.Join(",", strSqlValues);
                string insertSql = $"insert into {TableNameForSql()} ( {strSqlName} ) values ({strSqlValue});";

                return insertSql;
        }


        public virtual string GetDeleteSqlByWhereClause(string filter)
        {
            return $"DELETE FROM {TableNameForSql()} WHERE {filter}";
        }

        protected virtual string GetFieldList()
        {
            var propMappings = _tbMapping.PropMappings;
            var fieldListStr = string.Join(",", propMappings.Select(x => x.FieldName == x.PropertyName ?
                $"{PopulateName(x.PropertyName)}" : $"{PopulateName(x.FieldName)} as {PopulateName(x.PropertyName)}").ToArray());
            return fieldListStr;
        }

        protected List<Func<T, object>> FilterGetters(List<PropMapping> props)
        {
            var getters = PreCompileGetterHelper<T>.Getters;
            List<Func<T, object>> rs = new List<Func<T, object>>();
            props.ForEach(y => {
                rs.Add(getters.First(x => x.Key == y.PropertyName).Value);
            });
            return rs;
        }

        protected string BuildSelectStatement()
        {
            return $"Select {GetFieldList()} FROM {TableNameForSql()} ";
        }

        public string GetSelectAllSql()
        {
            var selectStatement = $"{BuildSelectStatement()} ; ";
            return selectStatement;
        }

        public virtual string GetCountSql(string filter)
        {
            StringBuilder sb = new StringBuilder();
            string where = "";
            if (string.IsNullOrEmpty(filter) == false && filter.Trim().Length > 0)
                where = $" WHERE {filter}";
            sb.AppendFormat($"SELECT COUNT(1) FROM {TableNameForSql()} {where};");
            return sb.ToString();
        }

        protected string GetKeyFilter()
        {
            var filterStatements = _tbMapping.PropMappings.Where(x => x.IsKey == true).Select(x => $" {PopulateName(x.FieldName)}={x.ParameterName}");
            return $" WHERE {string.Join(" AND ", filterStatements)}";
        }

        public  string GetSelectByKeySql()
        {
            var fieldListStr = string.Join(",", _tbMapping.PropMappings.Select(x => x.FieldName == x.PropertyName ? 
            $"{PopulateName(x.PropertyName)}" : $"{PopulateName(x.FieldName)} as {PopulateName(x.PropertyName)}").ToArray());
            var selectStatement = $"Select {fieldListStr} FROM {TableNameForSql()} {GetKeyFilter()}; ";

            return selectStatement;
        }
        public  string GetUpdateSql()
        {
            var fieldListStr = string.Join(",", _tbMapping.PropMappings.Where(x => x.AutoGenerate == false && x.IsKey == false).
                Select(x => $"{PopulateName(x.FieldName)} = @{x.PropertyName}").ToArray());
            var updateStatement = $"UPDATE {TableNameForSql()} SET {fieldListStr} {GetKeyFilter()} ;";
            return updateStatement;
        }

        public virtual KeyValuePair<string, DynamicParameters> BuildPartialUpdateSql(
            T keyModel,
            IReadOnlyDictionary<string, object?> changes)
        {
            if (keyModel == null)
                throw new ArgumentNullException(nameof(keyModel));
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));
            if (changes.Count == 0)
                throw new ArgumentException("No fields provided to update.", nameof(changes));

            var keyProps = _tbMapping.PropMappings.Where(x => x.IsKey).ToList();
            if (keyProps.Count == 0)
                throw new InvalidOperationException($"Model {typeof(T).Name} does not define a key field.");

            var parameters = new DynamicParameters();
            var setParts = new List<string>();

            foreach (var kv in changes)
            {
                var prop = _tbMapping.PropMappings.FirstOrDefault(p =>
                    p.PropertyName.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));

                if (prop == null)
                    throw new ArgumentException($"Unknown property '{kv.Key}' for model {typeof(T).Name}.", nameof(changes));
                if (prop.IsKey)
                    throw new ArgumentException($"Key property '{prop.PropertyName}' cannot be updated.", nameof(changes));
                if (prop.AutoGenerate)
                    continue;

                setParts.Add($"{PopulateName(prop.FieldName)} = @{prop.PropertyName}");
                parameters.Add(prop.PropertyName, kv.Value, ConvertToDbType(prop.PropertyType));
            }

            if (setParts.Count == 0)
                throw new ArgumentException("No updatable fields provided.", nameof(changes));

            foreach (var keyProp in keyProps)
            {
                var pi = typeof(T).GetProperty(keyProp.PropertyName);
                if (pi == null || pi.GetMethod == null)
                    throw new InvalidOperationException($"Key property '{keyProp.PropertyName}' is not readable.");

                var keyValue = pi.GetValue(keyModel);
                parameters.Add(keyProp.PropertyName, keyValue, ConvertToDbType(keyProp.PropertyType));
            }

            var where = string.Join(" AND ", keyProps.Select(k => $" {PopulateName(k.FieldName)}=@{k.PropertyName}"));
            var sql = $"UPDATE {TableNameForSql()} SET {string.Join(",", setParts)} WHERE {where};";
            return new KeyValuePair<string, DynamicParameters>(sql, parameters);
        }

        public virtual KeyValuePair<string, DynamicParameters> BuildPartialInsertSql(
            IReadOnlyDictionary<string, object?> changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));
            if (changes.Count == 0)
                throw new ArgumentException("No fields provided to insert.", nameof(changes));

            var parameters = new DynamicParameters();
            var cols = new List<string>();
            var vals = new List<string>();

            foreach (var kv in changes)
            {
                var prop = _tbMapping.PropMappings.FirstOrDefault(p =>
                    p.PropertyName.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));

                if (prop == null)
                    throw new ArgumentException($"Unknown property '{kv.Key}' for model {typeof(T).Name}.", nameof(changes));
                if (prop.AutoGenerate)
                    continue;

                cols.Add(PopulateName(prop.FieldName));
                vals.Add($"@{prop.PropertyName}");
                parameters.Add(prop.PropertyName, kv.Value, ConvertToDbType(prop.PropertyType));
            }

            if (cols.Count == 0)
                throw new ArgumentException("No insertable fields provided.", nameof(changes));

            var sql = $"INSERT INTO {TableNameForSql()} ({string.Join(",", cols)}) VALUES ({string.Join(",", vals)});";
            return new KeyValuePair<string, DynamicParameters>(sql, parameters);
        }

        public virtual string GetUpdateAndReturnSql()
        {
            return GetUpdateSql() + GetSelectByKeySql();
        }


        public virtual string GetDeleteByKey()
        {
            return $"DELETE FROM {TableNameForSql()} {GetKeyFilter()}";
        }



        protected abstract string BuildInsertedIdSql();
        //(Select Cast(SCOPE_IDENTITY() as INT)) for mssql and (select @@IDENTITY) for mysql

        public virtual string GetInsertAndReturnSql()
        {
            var insertSql = GetInsertSql();

            string returnFilter = "";
            var autoGeneratedMapping = _tbMapping.PropMappings.FirstOrDefault(x => x.AutoGenerate == true);
            if (autoGeneratedMapping != null)
            {
                returnFilter = $" WHERE {autoGeneratedMapping.FieldName}=  {BuildInsertedIdSql()}";
            }
            else
            {
                //var filterStatements = _tbMapping.PropMappings.Where(x => x.IsKey == true).Select(x => $" {x.FieldName}={x.ParameterName}");
                returnFilter = GetKeyFilter();// $" WHERE  { string.Join(" and ", filterStatements)}";
            }
            //var fieldListStr = string.Join(",", _tbMapping.PropMappings.Select(x => x.FieldName == x.PropertyName ? $"{PopulateName(x.PropertyName)}" : $"{PopulateName(x.FieldName)} as {PopulateName(x.PropertyName)}").ToArray());
            //var selectStatement = $"Select {GetFieldListStr()} FROM {TableNameForSql()} ";

            return insertSql + BuildSelectStatement() + returnFilter;
        }

       

        #region "Partition operation"

        public virtual string GetInsertSql<T1>(T1 model) where T1 : PartitionModelBase
        {
            string[] strSqlNames = _tbMapping.PropMappings.Where(x => x.AutoGenerate == false).Select(p => $"{PopulateName(p.FieldName)}").ToArray();
            string strSqlName = string.Join(",", strSqlNames);
            string[] strSqlValues = _tbMapping.PropMappings.Where(x => x.AutoGenerate == false).Select(P => $"{P.ParameterName}").ToArray();
            string strSqlValue = string.Join(",", strSqlValues);
            string insertSql = $"insert into {TableNameForSql<T1>(model)} ( {strSqlName} ) values ({strSqlValue});";

            return insertSql;
        }

        public string GetInsertAndReturnSql<T1>(T1 model) where T1 : PartitionModelBase
        {
            var insertSql = GetInsertSql<T1>(model);

            string returnFilter = "";
            var autoGeneratedMapping = _tbMapping.PropMappings.FirstOrDefault(x => x.AutoGenerate == true);
            if (autoGeneratedMapping != null)
            {
                returnFilter = $" WHERE {autoGeneratedMapping.FieldName}= {BuildInsertedIdSql()}";
            }
            else
            {
                //var filterStatements = _tbMapping.PropMappings.Where(x => x.IsKey == true).Select(x => $" {x.FieldName}={x.ParameterName}");
                returnFilter = GetKeyFilter();// $" WHERE  { string.Join(" and ", filterStatements)}";
            }
            var fieldListStr = string.Join(",", _tbMapping.PropMappings.Select(x => x.FieldName == x.PropertyName ? $"{PopulateName(x.PropertyName)}" : $"{PopulateName(x.FieldName)} as {PopulateName(x.PropertyName)}").ToArray());
            var selectStatement = $"Select {fieldListStr} FROM {TableNameForSql<T1>(model)} ";

            return insertSql + selectStatement + returnFilter;
        }


        public virtual string GetSelectByKeySql<T1>(T1 model) where T1 : PartitionModelBase
        {
            var fieldListStr = string.Join(",", _tbMapping.PropMappings.Select(x => x.FieldName == x.PropertyName ?
            $"{PopulateName(x.PropertyName)}" : $"{PopulateName(x.FieldName)} as {PopulateName(x.PropertyName)}").ToArray());
            var selectStatement = $"Select {fieldListStr} FROM {TableNameForSql<T1>(model)} {GetKeyFilter()}; ";

            return selectStatement;
        }


        public virtual string GetUpdateSql<T1>(T1 model) where T1 : PartitionModelBase
        {
            var autoGeneratedMapping = _tbMapping.PropMappings.FirstOrDefault(x => x.AutoGenerate == true);

            var fieldListStr = string.Join(",", _tbMapping.PropMappings.Where(x => x.AutoGenerate == false && x.IsKey == false).
                Select(x => $"{PopulateName(x.FieldName)} = @{x.PropertyName}").ToArray());
            var updateStatement = $"UPDATE {TableNameForSql<T1>(model)} SET {fieldListStr} {GetKeyFilter()} ;";
            return updateStatement;
        }

        public virtual string GetUpdateAndReturnSql<T1>(T1 model) where T1 : PartitionModelBase
        {
            return GetUpdateSql<T1>(model) + GetSelectByKeySql<T1>(model);
        }

        public virtual string GetDeleteByKey<T1>(T1 model) where T1 : PartitionModelBase
        {
            return $"DELETE FROM {TableNameForSql<T1>(model)} {GetKeyFilter()}";
        }

        #endregion

        #region "Create"

        public virtual string GetCreateSql()
        {
            var typeDecs = _tbMapping.PropMappings.Select(x =>
            {
                var typeDeclaration = "";
                var type = x.PropertyType;
                var nullable = " ";
                if (x.PropertyType.IsGenericType && x.PropertyType.Name == "Nullable`1")
                {
                    type = x.PropertyType.GetGenericArguments()[0];
                    nullable = " NULL ";
                }
                if (type == typeof(System.String))
                {
                    typeDeclaration = " Nvarchar(Max)";
                }

                if (type == typeof(System.Int32))
                {
                    typeDeclaration = " INT";
                }

                if (type == typeof(System.Byte))
                {
                    typeDeclaration = "TINYINT";
                }

                if (type == typeof(System.Int16))
                {
                    typeDeclaration = " SMALLINT";
                }

                if (type == typeof(System.Int64))
                {
                    typeDeclaration = " BIGINT";
                }

                if (type == typeof(System.Boolean))
                {
                    typeDeclaration = " BIT";
                }

                if (type == typeof(System.DateTime))
                {
                    typeDeclaration = " DateTime";
                }
                if (type == typeof(System.Decimal))
                {
                    typeDeclaration = " Decimal(18,8)";
                }
                string rs = $"{PopulateName(x.FieldName)} {typeDeclaration}";

                if (x.AutoGenerate)
                {
                    rs += " IDENTITY(1,1) ";
                }

                return rs + nullable;
            });

            var keyFieldNames = _tbMapping.PropMappings.Where(x => x.IsKey).Select(x => x.FieldName);
            typeDecs = typeDecs.Append("   Primary Key(" + string.Join(",", keyFieldNames) + ")");

            return $"CREATE TABLE {TableNameForSql()} ( {string.Join(",\r\n", typeDecs.ToArray())})";
        }
        #endregion

        public virtual WherePart GetWherePart(Expression<Func<T, bool>> expression)
        {
            return expression.ToSql<T>(TableNameForSql(), GetDbType());
        }

        protected virtual DBTypes GetDbType()
        {
            return DBTypes.SQLSERVER;
        }

        public virtual KeyValuePair<string, DynamicParameters> BuildQuery(Expression<Func<T, bool>> expression)
        {
            string selectStatement = BuildSelectStatement();
            var parameter = new DynamicParameters();
            var wherePart = GetWherePart(expression);
            string whereStatement = wherePart.HasSql ? $" WHERE {wherePart.Sql}" : string.Empty;
            foreach (var param in wherePart.Parameters)
            {
                parameter.Add(param.Key, param.Value, param.Type);
            }
            string sql = $"{selectStatement} {whereStatement}"; // sql.Replace("{where}", whereSql.HasSql ? $" WHERE {whereSql.Sql}" : string.Empty);

            return new KeyValuePair<string, DynamicParameters>(sql, parameter);
        }

        public KeyValuePair<string, DynamicParameters> BuildQueryTop(Expression<Func<T, bool>> expression, int number)
        {
            var parameter = new DynamicParameters();
            var wherePart = GetWherePart(expression);
            string whereStatement = wherePart.HasSql ? $" WHERE {wherePart.Sql}" : string.Empty;
            string sql = GetQueryTopRecordsSql(number, whereStatement);
            foreach (var param in wherePart.Parameters)
            {
                parameter.Add(param.Key, param.Value, param.Type);
            }
            return new KeyValuePair<string, DynamicParameters>(sql, parameter);
        }

        public KeyValuePair<string, DynamicParameters> BuildQuery(FilterOptions<T> filterOpt)
        {

            var parameter = new DynamicParameters();
            var wherePart = GetWherePart(filterOpt.WhereClause);
            string whereStatement = wherePart.HasSql ? $" WHERE {wherePart.Sql}" : string.Empty;
            string sql = GetCountSql(wherePart.Sql) + GetSelectByPagerSql(whereStatement, string.Join(" ",filterOpt.OrderByStatements), filterOpt.Pager.PageIndex, filterOpt.Pager.PageSize);
            foreach (var param in wherePart.Parameters)
            {
                parameter.Add(param.Key, param.Value, param.Type);
            }
            return new KeyValuePair<string, DynamicParameters>(sql, parameter);
        }

        public virtual KeyValuePair<string, DynamicParameters> BuildDeleteSql(Expression<Func<T, bool>> expression)
        {
            var parameter = new DynamicParameters();
            var wherePart = GetWherePart(expression);
            string whereStatement = wherePart.HasSql ? $" WHERE {wherePart.Sql}" : string.Empty;
            var sql =  $"Delete from {TableNameForSql()} {whereStatement}";
            foreach (var param in wherePart.Parameters)
            {
                parameter.Add(param.Key, param.Value, param.Type);
            }
            return new KeyValuePair<string, DynamicParameters>(sql, parameter);
        }

        protected virtual string BuildBatchInsertValuePart(List<string> values)
        {
            return " values " + string.Join(",", values.Select(x => '(' + x + ')'));
        }

        public KeyValuePair<string, DynamicParameters> BuildBatchInsertSql(List<T> models)
        {
            var props = _tbMapping.PropMappings.Where(x => x.AutoGenerate == false).ToList();
            string[] strSqlNames = props.Select(p => $"{PopulateName(p.FieldName)}").ToArray();
            string strSqlName = string.Join(",", strSqlNames);

            int i = 0;
            List<string> valueStatements = new List<string>();
            var parameter = new DynamicParameters();
            List<Func<T, object>> getters = FilterGetters(props);
            foreach (var model in models)
            {
                List<string> propNames = new List<string>();

                for (int k = 0; k < getters.Count(); k++)
                {
                    var p = props[k];
                    string propName = p.PropertyName + i.ToString();
                    propNames.Add('@' + propName);
                    var value = getters[k](model);

                    parameter.Add(propName, value, ConvertToDbType(p.PropertyType));
                }

                valueStatements.Add(string.Join(',', propNames));

                i++;
            }

            var valueStr = BuildBatchInsertValuePart(valueStatements);
            string insertSql = $"insert into {TableNameForSql()} ( {strSqlName} ) {valueStr};";
            return new KeyValuePair<string, DynamicParameters>(insertSql, parameter);
        }

        protected static DbType ConvertToDbType(Type propertyType)
        {
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propertyType = Nullable.GetUnderlyingType(propertyType);
            }

            if (propertyType == typeof(int))
                return DbType.Int32;
            if (propertyType == typeof(string))
                return DbType.String;
            if (propertyType == typeof(DateTime))
                return DbType.DateTime;
            if (propertyType == typeof(bool))
                return DbType.Boolean;
            // Add more type mappings as needed
            if (propertyType == typeof(byte))
                return DbType.Byte;
            if (propertyType == typeof(sbyte))
                return DbType.SByte;
            if (propertyType == typeof(short))
                return DbType.Int16;

            if (propertyType == typeof(uint))
                return DbType.UInt32;
            if (propertyType == typeof(long))
                return DbType.Int64;
            if (propertyType == typeof(ulong))
                return DbType.UInt64;
            if (propertyType == typeof(decimal))
                return DbType.Decimal;
            if (propertyType == typeof(double))
                return DbType.Double;

            throw new NotSupportedException($"Type mapping not supported for {propertyType.Name}");
        }
    }
}

