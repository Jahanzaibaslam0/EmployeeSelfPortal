using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace HRMS
{
    public static class WebFormsJson
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer
        {
            MaxJsonLength = int.MaxValue
        };

        public static List<T> DeserializeList<T>(string json)
        {
            List<T> list;
            TryDeserializeList(json, out list);
            return list ?? new List<T>();
        }

        /// <summary>
        /// Returns false when JSON is present but invalid (so callers can show an error
        /// instead of silently treating it as an empty list and wiping child rows).
        /// </summary>
        public static bool TryDeserializeList<T>(string json, out List<T> list)
        {
            list = new List<T>();
            if (string.IsNullOrWhiteSpace(json))
                return true;
            try
            {
                list = Serializer.Deserialize<List<T>>(json) ?? new List<T>();
                return true;
            }
            catch
            {
                list = new List<T>();
                return false;
            }
        }

        public static T Deserialize<T>(string json) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(json))
                return new T();
            try
            {
                return Serializer.Deserialize<T>(json) ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        public static string Serialize(object value)
        {
            return Serializer.Serialize(value);
        }
    }
}
