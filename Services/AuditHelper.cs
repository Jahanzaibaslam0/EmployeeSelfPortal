using System;
using System.Data.SqlClient;

namespace HRMS.Services
{
    public static class AuditHelper
    {
        public static void AddCreatedBy(SqlCommand cmd, int? userId)
        {
            cmd.Parameters.AddWithValue("@CreatedByUserID", userId.HasValue && userId.Value > 0 ? (object)userId.Value : DBNull.Value);
        }

        public static void AddModifiedBy(SqlCommand cmd, int? userId)
        {
            cmd.Parameters.AddWithValue("@ModifiedByUserID", userId.HasValue && userId.Value > 0 ? (object)userId.Value : DBNull.Value);
        }
    }
}
