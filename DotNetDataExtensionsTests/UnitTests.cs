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
        private readonly Customer _customer = new Customer(); // Here only for 'nameof' operator use

        public UnitTests()
        {
            _dtTest = new DataTable();
            _dtTest.ReadXml(@"..\..\TestData.xml");
        }

        private void TestAllCustomerPropertiesById(Customer c)
        {
            var dr = _dtTest.Select($"{nameof(c.Id)} = {c.Id}")[0];
            TestAllCustomerPropertiesById(c, dr);
        }

        private void TestAllCustomerPropertiesById(Customer c, DataRow dr)
        {
            Assert.That(c.FirstName, Is.EqualTo(dr.ConvertTo<string>(nameof(c.FirstName))));
            Assert.That(c.LastName, Is.EqualTo(dr.ConvertTo<string>(nameof(c.LastName))));
            Assert.That(c.Email, Is.EqualTo(dr.ConvertTo<string>(nameof(c.Email))));
            Assert.That(c.PhoneNumber, Is.EqualTo(dr.ConvertTo<string>(nameof(c.PhoneNumber))));
            Assert.That(c.Address, Is.EqualTo(dr.ConvertTo<string>(nameof(c.Address))));
            Assert.That(c.City, Is.EqualTo(dr.ConvertTo<string>(nameof(c.City))));
            Assert.That(c.State, Is.EqualTo(dr.ConvertTo<string>(nameof(c.State))));
            Assert.That(c.Zip, Is.EqualTo(dr.ConvertTo(nameof(c.Zip), 0)));
            Assert.That(c.RewardsPoints, Is.EqualTo(dr[nameof(c.RewardsPoints)]
                == DBNull.Value ? null : dr[nameof(c.RewardsPoints)]));
        }

        [TestCase]
        public void DataReader_ReturnsCorrectListCount()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                Assert.That(customerList.Count, Is.EqualTo(_dtTest.Rows.Count));
            }
        }

        [TestCase]
        public void DataTable_ReturnsCorrectListCount()
        {
            var customerList = _dtTest.MapTo<Customer>();
            Assert.That(customerList.Count, Is.EqualTo(_dtTest.Rows.Count));
        }

        [TestCase]
        public void DataReader_AreAllPropertiesMappedCorrectFromAllRows()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                foreach (var c in customerList)
                {
                    TestAllCustomerPropertiesById(c);
                }
            }
        }

        [TestCase]
        public void DataTable_AreAllPropertiesMappedCorrectFromAllRows()
        {
            var customerList = _dtTest.MapTo<Customer>();
            foreach (var c in customerList)
            {
                 TestAllCustomerPropertiesById(c);
            }
        }

        [TestCase]
        public void DataRow_AreAllPropertiesMappedCorrectFromAllRows()
        {
            foreach (DataRow dr in _dtTest.Rows)
            {
                TestAllCustomerPropertiesById(dr.MapTo<Customer>(), dr);
            }
        }

        [TestCase]
        public void DataReader_DoesNullableTypeSetNullCorrectly()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                var cust = customerList.First(c => c.Id == 2);
                Assert.IsNull(cust.RewardsPoints);
            }
        }

        [TestCase]
        public void DataTable_DoesNullableTypeSetNullCorrectly()
        {
            var customerList = _dtTest.MapTo<Customer>();
            var cust = customerList.First(c => c.Id == 2);
            Assert.IsNull(cust.RewardsPoints);
        }

        [TestCase]
        public void DataRow_DoesNullableTypeSetNullCorrectly()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 2");
            var cust = dr[0].MapTo<Customer>();
            Assert.IsNull(cust.RewardsPoints);
        }

        [TestCase]
        public void DataReader_DoesNonNullableTypeSetDefaultCorrectly()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                var cust = customerList.First(c => c.Id == 2);
                Assert.That(cust.Zip, Is.EqualTo(0));
            }
        }

        [TestCase]
        public void DataTable_DoesNonNullableTypeSetDefaultCorrectly()
        {
            var customerList = _dtTest.MapTo<Customer>();
            var cust = customerList.First(c => c.Id == 2);
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataRow_DoesNonNullableTypeSetDefaultCorrectly()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 2");
            var cust = dr[0].MapTo<Customer>();
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataReader_DoesDBNullOverrideCorrectly()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo(new Customer { Zip = 0 });
                var cust = customerList.First(c => c.Id == 2);
                Assert.That(cust.Zip, Is.EqualTo(0));
            }
        }

        [TestCase]
        public void DataTable_DoesDBNullOverrideCorrectly()
        {
            var customerList = _dtTest.MapTo(new Customer { Zip = 0 });
            var cust = customerList.First(c => c.Id == 2);
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataRow_DoesDBNullOverrideCorrectly()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 2");
            var cust = dr[0].MapTo(new Customer { Zip = 0 });
            Assert.That(cust.Zip, Is.EqualTo(0));
        }

        [TestCase]
        public void DataReader_DoesDBNullReturnCorrectlyForString()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>();
                var cust = customerList.First(c => c.Id == 3);
                Assert.That(cust.PhoneNumber, Is.EqualTo(string.Empty));
            }
        }

        [TestCase]
        public void DataTable_DoesDBNullReturnCorrectlyForString()
        {
            var customerList = _dtTest.MapTo<Customer>();
            var cust = customerList.First(c => c.Id == 3);
            Assert.That(cust.PhoneNumber, Is.EqualTo(string.Empty));
        }

        [TestCase]
        public void DataRow_DoesDBNullReturnCorrectlyForString()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 3");
            var cust = dr[0].MapTo<Customer>();
            Assert.That(cust.PhoneNumber, Is.EqualTo(string.Empty));
        }

        [TestCase]
        public void DataReader_DoesDBNullReturnCorrectlyForStringWithNullOverrideTurnedOff()
        {
            using (var reader = _dtTest.CreateDataReader())
            {
                var customerList = reader.MapTo<Customer>(false);
                var cust = customerList.First(c => c.Id == 3);
                Assert.IsNull(cust.PhoneNumber);
            }
        }

        [TestCase]
        public void DataTable_DoesDBNullReturnCorrectlyForStringWithNullOverrideTurnedOff()
        {
            var customerList = _dtTest.MapTo<Customer>(false);
            var cust = customerList.First(c => c.Id == 3);
            Assert.IsNull(cust.PhoneNumber);
        }

        [TestCase]
        public void DataRow_DoesDBNullReturnCorrectlyForStringWithNullOverrideTurnedOff()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 3");
            var cust = dr[0].MapTo<Customer>(false);
            Assert.IsNull(cust.PhoneNumber);
        }

        [TestCase]
        public void ConvertTo_DoesReturnEmptyStringForDBNull()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 3");
            var phone = dr[0].ConvertTo<string>(nameof(_customer.PhoneNumber));
            Assert.That(phone, Is.EqualTo(string.Empty));
        }

        [TestCase]
        public void ConvertTo_DoesNullValueOverrideCorrectly()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 2");
            var zip = dr[0].ConvertTo(nameof(_customer.Zip), 11111);
            Assert.That(zip, Is.EqualTo(11111));
        }

        [TestCase]
        public void ConvertTo_DoesFieldWithValueMapCorrectly()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 1");
            var firstName = dr[0].ConvertTo<string>(nameof(_customer.FirstName));
            Assert.That(firstName, Is.EqualTo(dr[0][nameof(_customer.FirstName)]));
        }

        [TestCase]
        public void ConvertTo_DoesDbNullStringOverrideToNull()
        {
            var dr = _dtTest.Select($"{nameof(_customer.Id)} = 2");
            var phoneNumber = dr[0].ConvertTo<string>(nameof(_customer.PhoneNumber), null);
            Assert.IsNull(phoneNumber);
        }
    }
}