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
    // http://tinyurl.com/gsbjk52 (see answer 21)
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
            => t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;

        private static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
                action?.Invoke(element);
        }

        private static T ClonePoco<T>(TypeAccessor accessor, T defaultPoco) where T : class
        {
            // Copy the values of the POCO passed in to a new POCO.
            var clonedPoco = New<T>.Instance();
            accessor.GetMembers()
                    .ForEach(c => accessor[clonedPoco, c.Name] = accessor[defaultPoco, c.Name]);
            return clonedPoco;
        }

        private static T NullStringsToEmpty<T>(TypeAccessor accessor, T poco) where T : class
        {
            // Set null strings in the POCO should to empty strings.
            accessor.GetMembers()
                    .Where(p => p.Type == typeof(string))
                    .Where(p => accessor[poco, p.Name] == null)
                    .ForEach(prop => accessor[poco, prop.Name] = string.Empty);
            return poco;
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

        public static T MapTo<T>(this DataRow row, T defaultPoco, bool nullStringsToEmpty = true) where T : class
        {
            // Third party from Nuget - faster than reflection.
            var accessor = TypeAccessor.Create(typeof(T));

            // We only want to map to properties in our POCO with non null values
            // and return the existing object values for the rest.
            // Properly handle nullable types in the POCO when converting the data type.
           accessor.GetMembers()
                    .Where(p => row.Table.Columns.Contains(p.Name))
                    .Where(p => !row.IsNull(p.Name))
                    .ForEach(p => accessor[defaultPoco, p.Name]
                        = Convert.ChangeType(row[p.Name],
                            Nullable.GetUnderlyingType(p.Type) ?? p.Type, CultureInfo.CurrentCulture));
                 
            // Set null strings to empty in the POCO unless default parameter was overridden.
            return nullStringsToEmpty ? NullStringsToEmpty(accessor, defaultPoco) : defaultPoco;
        }

        public static List<T> MapTo<T>(this DataTable table, bool nullStringsToEmpty = true)
            where T : class => table.MapTo(New<T>.Instance(), nullStringsToEmpty);

        public static List<T> MapTo<T>(this DataTable table, T defaultPoco, bool nullStringsToEmpty = true) where T : class
        {
            // Third party from nuget - faster than reflection.
            var accessor = TypeAccessor.Create(typeof(T));

            // Use the DataRow MapTo for each row in the table with a clone of the default POCO.
            var pocoList = new List<T>();
            pocoList.AddRange(table.Rows.Cast<DataRow>()
                    .Select(row => new { row, poco = ClonePoco(accessor, defaultPoco), nullStringsToEmpty })
                    .Select(@t => MapTo(@t.row, @t.poco, @t.nullStringsToEmpty)));
            return pocoList;
        }

        public static List<T> MapTo<T>(this IDataReader reader, bool nullStringsToEmpty = true)
            where T : class => reader.MapTo(New<T>.Instance(), nullStringsToEmpty);

        public static List<T> MapTo<T>(this IDataReader reader, T defaultPoco, bool nullStringsToEmpty = true) where T : class
        {
            // Third party from nuget - faster than reflection.
            var accessor = TypeAccessor.Create(typeof(T));

            // Cache the field names in the reader for use in our while loop for efficiency.
            var readerFieldLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // store name and ordinal
            for (var i = 0; i < reader.FieldCount; i++)
                readerFieldLookup.Add(reader.GetName(i), i);

            // Ignore extra fields in the DataReader and ignore extra properties in our POCO.
            var pocoList = new List<T>();
            while (reader.Read())
            {
                // We need a clone of the default POCO for each record.
                var clonedPoco = ClonePoco(accessor, defaultPoco);
                
                // We only want to map to properties in our POCO with non null values
                // and return the existing object values for the rest.
                // Properly handle nullable types in the POCO when converting the data type.
                accessor.GetMembers()
                        .Where(p => readerFieldLookup.ContainsKey(p.Name))
                        .Where(p => !reader.IsDBNull(readerFieldLookup[p.Name]))
                        .ForEach(p => accessor[clonedPoco, p.Name]
                            = Convert.ChangeType(reader.GetValue(readerFieldLookup[p.Name]),
                                Nullable.GetUnderlyingType(p.Type) ?? p.Type, CultureInfo.CurrentCulture));

                // Set null strings to empty in the POCO unless default parameter was overridden
                pocoList.Add(nullStringsToEmpty ? NullStringsToEmpty(accessor, clonedPoco) : clonedPoco);
            }
            return pocoList;
        }
    }
}