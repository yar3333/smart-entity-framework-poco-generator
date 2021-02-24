using System;
using System.Data.Common;

namespace SmartEntityFrameworkPocoGenerator
{
    abstract class BaseDatabaseConnection
    {
        protected DbConnection connection;

        protected BaseDatabaseConnection(DbConnection connection)
        {
            this.connection = connection;

            try { connection.Open(); } catch (Exception e) { throw new GeneratorException(e.Message); }
        }

        public void Dispose()
        {
            connection?.Dispose();
            connection = null;
        }

        public abstract string[] getTables();

        public abstract ColumnInfo[] getColumns(string table);

        public virtual string dbTypeToCs(string dataType, int len, bool nullable, int? precision, int? scale) { return dataType; }

        public virtual string tableNameToCs(string table) { return table; }
        
        public virtual string columnNameToCs(string name) { return name; }
    }
}
