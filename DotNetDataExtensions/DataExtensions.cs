using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using FastMember;

// These extensions require the third party FastMember.dll that
// uses emitted IL instead of reflection for significant performance gains.
// Use the NuGet package.
namespace DataExtensions
{
    // Shamelessly stolen from StackOverflow titled:
    // Fast creation of objects instead of Activator.CreateInstance(type)
    internal static class New<T>
    {
        internal static readonly Func<T> Instance = Creator();

        private static Func<T> Creator()
        {
            var t = typeof(T);
            if (t == typeof(string))
                return Expression.Lambda<Func<T>>(Expression.Constant(string.Empty)).Compile();

            if (t.HasDefaultConstructor())
                return Expression.Lambda<Func<T>>(Expression.New(t)).Compile();

            return () => (T)FormatterServices.GetUninitializedObject(t);
        }
    }

    public static class DataExtensionMethods
    {
        internal static bool HasDefaultConstructor(this Type t) => t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;

        private static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
                action?.Invoke(element);
        }

        private static T ClonePoco<T>(TypeAccessor accessor, T defaultEntity) where T : class
        {
            // Copy the values of the POCO passed in to a new POCO
            var entity = New<T>.Instance();
            accessor.GetMembers()
                    .ForEach(c => accessor[entity, c.Name] = accessor[defaultEntity, c.Name]);
            return entity;
        }

        private static T NullStringsToEmpty<T>(TypeAccessor accessor, T entity) where T : class
        {
            // Null strings in the POCO should be set to an empty string
            accessor.GetMembers()
                    .Where(p => p.Type == typeof(string))
                    .Where(p => accessor[entity, p.Name] == null)
                    .ForEach(prop => accessor[entity, prop.Name] = string.Empty);
            return entity;
        }

        public static T ConvertTo<T>(this DataRow row, string columnName)
            => (T)Convert.ChangeType(row[columnName], typeof(T), CultureInfo.CurrentCulture);

        public static T ConvertTo<T>(this DataRow row, string columnName, T defaultValue)
        {
            if (row[columnName] is DBNull) return defaultValue;
            return row.ConvertTo<T>(columnName);
        }

        public static T MapTo<T>(this DataRow row, bool nullStringsToEmpty = true)
            where T : class => row.MapTo(New<T>.Instance(), nullStringsToEmpty);

        public static T MapTo<T>(this DataRow row, T defaultEntity, bool nullStringsToEmpty = true) where T : class
        {
            // Third party from Nuget - faster than reflection
            var accessor = TypeAccessor.Create(typeof(T));

            // We only want to map to properties in our entity with non null values
            // and return the existing object values for the rest.
            // Properly handle nullable types in the POCO when converting the data type
            accessor.GetMembers()
                    .Where(p => row.Table.Columns.Contains(p.Name))
                    .Where(p => !row.IsNull(p.Name))
                    .ForEach(prop => accessor[defaultEntity, prop.Name]
                        = Convert.ChangeType(row[prop.Name],
                            Nullable.GetUnderlyingType(prop.Type) ?? prop.Type, CultureInfo.CurrentCulture));

            // Null strings in the POCO should be set to an empty string
            if (nullStringsToEmpty)
                defaultEntity = NullStringsToEmpty(accessor, defaultEntity);

            return defaultEntity;
        }

        public static List<T> MapTo<T>(this DataTable table, bool nullStringsToEmpty = true)
            where T : class => table.MapTo(New<T>.Instance(), nullStringsToEmpty);

        public static List<T> MapTo<T>(this DataTable table, T defaultEntity, bool nullStringsToEmpty = true) where T : class
        {
            // Third party from nuget - faster than reflection
            var accessor = TypeAccessor.Create(typeof(T));

            var entityList = new List<T>();
            entityList.AddRange(table.Rows.Cast<DataRow>()
                                          .Select(row => new { row, entity = ClonePoco(accessor, defaultEntity), nullStringsToEmpty })
                                          .Select(@t => MapTo(@t.row, @t.entity, @t.nullStringsToEmpty)));
            return entityList;
        }

        public static List<T> MapTo<T>(this IDataReader reader, bool nullStringsToEmpty = true)
            where T : class => reader.MapTo(New<T>.Instance(), nullStringsToEmpty);

        public static List<T> MapTo<T>(this IDataReader reader, T defaultEntity, bool nullStringsToEmpty = true) where T : class
        {
            // Third party from nuget - faster than reflection
            var accessor = TypeAccessor.Create(typeof(T));

            // Cache the field names in the reader for use in our while loop for efficiency
            var readerFieldLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // store name and ordinal
            for (var i = 0; i < reader.FieldCount; i++)
                readerFieldLookup.Add(reader.GetName(i), i);

            // Ignore extra fields in the DataReader and ignore extra properties in our POCO.
            var entityList = new List<T>();
            while (reader.Read())
            {
                var entity = ClonePoco(accessor, defaultEntity);
                // We only want to map to properties in our entity with non null values
                // and return the existing object values for the rest.
                // Properly handle nullable types in the POCO when converting the data type
                accessor.GetMembers()
                        .Where(p => readerFieldLookup.ContainsKey(p.Name))
                        .Where(p => !reader.IsDBNull(readerFieldLookup[p.Name]))
                        .ForEach(p => accessor[entity, p.Name]
                            = Convert.ChangeType(reader.GetValue(readerFieldLookup[p.Name]),
                                Nullable.GetUnderlyingType(p.Type) ?? p.Type, CultureInfo.CurrentCulture));

                // Null strings in the POCO should be set to an empty string
                if (nullStringsToEmpty)
                    entity = NullStringsToEmpty(accessor, entity);

                entityList.Add(entity);
            }
            return entityList;
        }
    }
}