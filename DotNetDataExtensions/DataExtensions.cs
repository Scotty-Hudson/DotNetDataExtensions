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
        internal static bool HasDefaultConstructor(this Type t)
        {
            return t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;
        }

        private static T ClonePoco<T>(TypeAccessor accessor, T defaultEntity)
        {
            // Copy the values of the POCO passed in to a new POCO
            var entity = New<T>.Instance();
            accessor.GetMembers()
                    .Select(c => accessor[entity, c.Name] = accessor[defaultEntity, c.Name]);

            return entity;
        }

        public static T ConvertTo<T>(this DataRow row, string columnName)
        {
            return (T)Convert.ChangeType(row[columnName], typeof(T), CultureInfo.CurrentCulture);
        }

        public static T ConvertTo<T>(this DataRow row, string columnName, T defaultValue)
        {
            if (row[columnName] is DBNull) return defaultValue;
            return row.ConvertTo<T>(columnName);
        }

        public static T MapTo<T>(this DataRow row) where T : class
        {
            return row.MapTo(New<T>.Instance());
        }

        public static T MapTo<T>(this DataRow row, T defaultEntity) where T : class
        {
            // Third party from nuget - faster than reflection
            var accessor = TypeAccessor.Create(typeof(T));

            foreach (var prop in accessor.GetMembers())
            {
                // We only want to map to properties in our entity with non null values
                // and return the existing object values for the rest.
                if (!row.Table.Columns.Contains(prop.Name) || row[prop.Name] is DBNull)
                {
                    // Most of the time, I prefer empty strings over nulls
                    if (prop.Type == typeof(string))
                        accessor[defaultEntity, prop.Name] = string.Empty;

                    continue;
                }

                // Nullable fields in the POCO should be null if the value in the DataRow is null
                var t = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;
                var safe = row.IsNull(prop.Name)
                    ? null
                    : Convert.ChangeType(row[prop.Name], t, CultureInfo.CurrentCulture);
                accessor[defaultEntity, prop.Name] = safe;
            }
            return defaultEntity;
        }

        public static List<T> MapTo<T>(this DataTable table) where T : class
        {
            return table.MapTo(New<T>.Instance());
        }

        public static List<T> MapTo<T>(this DataTable table, T defaultEntity) where T : class
        {
            // Third party from nuget - faster than reflection
            var accessor = TypeAccessor.Create(typeof(T));

            var entityList = new List<T>();
            entityList.AddRange(table.Rows.Cast<DataRow>()
                                          .Select(row => new { row, entity = ClonePoco(accessor, defaultEntity) })
                                          .Select(@t => MapTo(@t.row, @t.entity)));
            return entityList;
        }

        public static List<T> MapTo<T>(this IDataReader reader) where T : class
        {
            return reader.MapTo(New<T>.Instance());
        }

        public static List<T> MapTo<T>(this IDataReader reader, T defaultEntity) where T : class
        {
            // Third party from nuget - faster than reflection
            var accessor = TypeAccessor.Create(typeof(T));

            // Cache the field names in the reader for efficiency
            var readerFieldLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // store name and ordinal
            for (var i = 0; i < reader.FieldCount; i++)
                readerFieldLookup.Add(reader.GetName(i), i);

            // Ignore extra fields in the DataReader and ignore extra properties in our POCO.
            var entityList = new List<T>();
            while (reader.Read())
            {
                var entity = New<T>.Instance();
                foreach (var prop in accessor.GetMembers())
                {
                    // Set our new object of type T's value for this property to the value of the instance passed in.
                    accessor[entity, prop.Name] = accessor[defaultEntity, prop.Name];

                    // We only want to map to properties in our entity with non null values
                    // and return the existing object's values for the rest.
                    if (!readerFieldLookup.ContainsKey(prop.Name) || reader[prop.Name] is DBNull)
                    {
                        // Most of the time, I prefer empty strings over nulls
                        if (prop.Type == typeof(string))
                            accessor[defaultEntity, prop.Name] = string.Empty;

                        continue;
                    }

                    // Nullable fields in the POCO should be null if the value in the DataRow is null
                    var t = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;
                    var safe = reader.IsDBNull(readerFieldLookup[prop.Name])
                        ? null
                        : Convert.ChangeType(reader.GetValue(readerFieldLookup[prop.Name]), t, CultureInfo.CurrentCulture);
                    accessor[entity, prop.Name] = safe;
                }
                entityList.Add(entity);
            }
            return entityList;
        }
    }
}