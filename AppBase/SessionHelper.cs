using System;
using System.Web.SessionState;

namespace HRMS
{
    public static class SessionHelper
    {
        public static int? GetInt32(HttpSessionState session, string key)
        {
            var val = session[key];
            if (val == null) return null;
            return Convert.ToInt32(val);
        }

        public static void SetInt32(HttpSessionState session, string key, int? value)
        {
            if (value.HasValue)
                session[key] = value.Value;
            else
                session.Remove(key);
        }

        public static string GetString(HttpSessionState session, string key)
        {
            return session[key] as string ?? "";
        }

        public static void SetString(HttpSessionState session, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                session.Remove(key);
            else
                session[key] = value;
        }
    }
}
