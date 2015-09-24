# DotNetDataExtensions

## MapTo Extension

**Description:** Maps a IDataReader or DataTable to a list of plain old CLR objects. It will also map a DataRow to a plain old CLR object. The column names to be mapped to the POCO must match (case insensitive) the names of the properties in the POCO. 

Any properties in the POCO that are not in the IDataReader, DataTable, or DataRow or DBNull will be intialized to the property's default value for that particular data type with the exception of a string which will be intailized as an empty string. Any columns in the IDataReader, DataTable, or DataRow that are not in the POCO will be ignored. 

One can override a property's default value by passing in a default instance of the POCO intialized with default values set.

**Efficiency:**
These extensions use the [FastMember] (https://www.nuget.org/packages/FastMember) NuGet package to iterate through properties using emitted IL instead of reflection, which is considerably faster. We also use a [compiled lambda expression] (http://stackoverflow.com/questions/6582259/fast-creation-of-objects-instead-of-activator-createinstancetype?rq=1) taken from stack overflow (see answer 17) to create objects instead of using Activator.CreateInstance(). This is very close to the speed of using the new operator.

**Examples:**

*IDataReader:*

```c#
var customerList = reader.MapTo<Customer>();
```

or

```c#
var customerList = reader.MapTo(new Customer { Zip = 0 });
```

*DataTable:*

```c#
var customerList = dataTable.MapTo();
```

or

```
var customerList = dataTable.MapTo(new Customer { FirstName="Please", 
												  LastName = "Enter",
                                                  Zip=0 
                                                 });
```

*DataRow:*

```c#
var customer = row.MapTo<Customer>();
```
or

```
var customer = row.MapTo( new Customer { Zip = 0 });
```

## ConvertTo Extension

**Description:** This extends DataRow and will convert a column's value to the .Net type specified. One can also pass a default value to return if the column is DBNull. Unlike MapTo, in this extension, a DBNull for a string will return null.

**Examples:**

*DataRow:*

```
var customerId = row.ConvertTo<long>("CustomerId");
```
or
```
var customerId = row.ConvertTo("CustomerId", 0);
```

### Dependancies:

These DataExtensions depend on the NuGet package *FastMember*. 

The unit tests are using the NUnit framework.

