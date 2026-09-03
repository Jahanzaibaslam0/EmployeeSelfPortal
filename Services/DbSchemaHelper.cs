using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;

namespace HRMS.Services
{
    /// <summary>
    /// Detects physical columns so DML can adapt when production DBs were created manually.
    /// </summary>
    public static class DbSchemaHelper
    {
        private static readonly ConcurrentDictionary<string, bool> Cache =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static bool HasColumn(SqlConnection conn, SqlTransaction tx, string tableName, string columnName)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
                return false;

            var key = tableName.Trim() + "." + columnName.Trim();
            bool cached;
            if (Cache.TryGetValue(key, out cached))
                return cached;

            using (var cmd = new SqlCommand(@"
SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.name = @TableName AND c.name = @ColumnName
) THEN 1 ELSE 0 END;", conn))
            {
                if (tx != null) cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@TableName", tableName.Trim());
                cmd.Parameters.AddWithValue("@ColumnName", columnName.Trim());
                var found = Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                Cache[key] = found;
                return found;
            }
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}
