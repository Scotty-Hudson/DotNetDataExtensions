using System;
using System.Linq;
using System.Data;
using NUnit.Framework;
using DataExtensions;

namespace DotNetDataExtensionsTests
{
    public class UnitTests
    {
        private readonly DataTable _dtTest;

        public UnitTests()
        {
            _dtTest = new DataTable();
            _dtTest.ReadXml(@"..\..\TestData.xml");
        }

        private void TestAllCustomerPropertiesById(Customer cust)
        {
            var dr = _dtTest.Select($"{nameof(cust.CustomerId)} = {cust.CustomerId}")[0];
            Assert.That(cust.FirstName, Is.EqualTo(dr.ConvertTo<string>(nameof(cust.FirstName))));
            Assert.That(cust.LastName, Is.EqualTo(dr.ConvertTo<string>(nameof(cust.LastName))));
            Assert.That(cust.Email, Is.EqualTo(dr.ConvertTo<string>(nameof(cust.Email))));
            Assert.That(cust.PhoneNumber, Is.EqualTo(dr.ConvertTo<string>(nameof(cust.PhoneNumber))));
            Assert.That(cust.Address, Is.EqualTo(dr.ConvertTo<string>(nameof(cust.Address))));
            Assert.That(cust.City, Is.EqualTo(dr.ConvertTo<string>(nameof(cust.City))));
            Assert.That(cust.State, Is.EqualTo(dr.ConvertTo<string>(nameof(cust.State))));
            Assert.That(cust.Zip, Is.EqualTo(dr.ConvertTo<int>(nameof(cust.Zip))));
            Assert.That(cust.RewardsPoints, Is.EqualTo((Nullable<decimal>)dr[nameof(cust.RewardsPoints)]));
        }

        [TestCase]
        public void DataReader_ReturnsCorrectListCount()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                Assert.That(customerList.Count, Is.EqualTo(3));
            }
        }

        [TestCase]
        public void DataTable_ReturnsCorrectListCount()
        {
            var customerList = _dtTest.MapTo<Customer>();
            Assert.That(customerList.Count, Is.EqualTo(3));
        }

        [TestCase]
        public void DataReader_AreFieldsCorrectInFirstRecord()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                var cust = customerList.First(c => c.CustomerId == 1);
                TestAllCustomerPropertiesById(cust);
            }
        }
        
        [TestCase]
        public void DataTable_AreFieldsCorrectInFirstRecord()
        {
            var customerList = _dtTest.MapTo<Customer>();
            var cust = customerList.First(c => c.CustomerId == 1);
            TestAllCustomerPropertiesById(cust);
        }

        [TestCase]
        public void DataRow_AreFieldsCorrectInFirstRecord()
        {
            var dr = _dtTest.Select("CustomerId = 1");
            var cust = dr[0].MapTo<Customer>();
            TestAllCustomerPropertiesById(cust);
        }

        [TestCase]
        public void DataReader_DoesNullableTypeSetNullCorrectly()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                var cust = customerList.First(c => c.CustomerId == 2);
                Assert.IsNull(cust.RewardsPoints);
            }
        }

        [TestCase]
        public void DataTable_DoesNullableTypeSetNullCorrectly()
        {
            var customerList = _dtTest.MapTo<Customer>();
            var cust = customerList.First(c => c.CustomerId == 2);
            Assert.IsNull(cust.RewardsPoints);
        }

        [TestCase]
        public void DataRow_DoesNullableTypeSetNullCorrectly()
        {
            var dr = _dtTest.Select("CustomerId = 2");
            var cust = dr[0].MapTo<Customer>();
            Assert.IsNull(cust.RewardsPoints);
        }

        [TestCase]
        public void DataReader_DoesNonNullableTypeSetDefaultCorrectly()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                var cust = customerList.First(c => c.CustomerId == 2);
                Assert.That(cust.Zip, Is.EqualTo(0));
            }
        }

        [TestCase]
        public void DataTable_DoesNonNullableTypeSetDefaultCorrectly()
        {
            var customerList = _dtTest.MapTo<Customer>();
            var cust = customerList.First(c => c.CustomerId == 2);
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataRow_DoesNonNullableTypeSetDefaultCorrectly()
        {
            var dr = _dtTest.Select("CustomerId = 2");
            var cust = dr[0].MapTo<Customer>();
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataReader_DoesDBNullOverrideCorrectly()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo(new Customer { Zip = 0 });
                var cust = customerList.First(c => c.CustomerId == 2);
                Assert.That(cust.Zip, Is.EqualTo(0));
            }
        }

        [TestCase]
        public void DataTable_DoesDBNullOverrideCorrectly()
        {
            var customerList = _dtTest.MapTo(new Customer { Zip = 0 });
            var cust = customerList.First(c => c.CustomerId == 2);
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataRow_DoesDBNullOverrideCorrectly()
        {
            var dr = _dtTest.Select("CustomerId = 2");
            var cust = dr[0].MapTo(new Customer { Zip = 0 });
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataReader_DoesDBNullReturnCorrectlyForString()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                var cust = customerList.First(c => c.CustomerId == 3);
                Assert.That(cust.PhoneNumber, Is.EqualTo(string.Empty));
            }
        }

        [TestCase]
        public void DataTable_DoesDBNullReturnCorrectlyForString()
        {
            var customerList = _dtTest.MapTo<Customer>();
            var cust = customerList.First(c => c.CustomerId == 3);
            Assert.That(cust.PhoneNumber, Is.EqualTo(string.Empty));
        }

        [TestCase]
        public void DataRow_DoesDBNullReturnCorrectlyString()
        {
            var dr = _dtTest.Select("CustomerId = 3");
            var cust = dr[0].MapTo<Customer>();
            Assert.That(cust.PhoneNumber, Is.EqualTo(string.Empty));
        }

        [TestCase]
        public void DataReader_DoesDBNullReturnCorrectlyForStringWithNullOverrideTurnedOff()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>(false);
                var cust = customerList.First(c => c.CustomerId == 3);
                Assert.IsNull(cust.PhoneNumber);
            }
        }

        [TestCase]
        public void DataTable_DoesDBNullReturnCorrectlyForStringWithNullOverrideTurnedOff()
        {
            var customerList = _dtTest.MapTo<Customer>(false);
            var cust = customerList.First(c => c.CustomerId == 3);
            Assert.IsNull(cust.PhoneNumber);
        }

        [TestCase]
        public void DataRow_DoesDBNullReturnCorrectlyForStringWithNullOverrideTurnedOff()
        {
            var dr = _dtTest.Select("CustomerId = 3");
            var cust = dr[0].MapTo<Customer>(false);
            Assert.IsNull(cust.PhoneNumber);
        }
    }

    public class Customer
    {
        public long CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Zip { get; set; }
        public decimal? RewardsPoints { get; set; }
    }
}
