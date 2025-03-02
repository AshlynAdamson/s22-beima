﻿using MongoDB.Bson;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static BEIMA.Backend.FT.TestObjects;

namespace BEIMA.Backend.FT
{
    [TestFixture]
    public class DeviceTypeFT : FunctionalTestBase
    {
        #region SetUp and Teardown

        [SetUp]
        public async Task SetUp()
        {

            // Delete all the devices in the database.
            var deviceList = await TestClient.GetDeviceList();
            foreach (var device in deviceList)
            {
                if (device?.Id is not null)
                {
                    await TestClient.DeleteDevice(device.Id);
                }
            }
            // Delete all the device types in the database
            var deviceTypeList = await TestClient.GetDeviceTypeList();
            foreach (var deviceType in deviceTypeList)
            {
                if (deviceType?.Id is not null)
                {
                    await TestClient.DeleteDeviceType(deviceType.Id);
                }
            }
        }

        #endregion SetUp and Teardown

        #region Device Type GET Tests

        [TestCase("xxx")]
        [TestCase("1234")]
        [TestCase("1234567890abcdef1234567x")]
        public void InvalidId_DeviceTypeGet_ReturnsInvalidId(string id)
        {
            var ex = Assert.ThrowsAsync<BeimaException>(async () =>
                await TestClient.GetDeviceType(id)
            );
            Assert.IsNotNull(ex);
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [TestCase("")]
        [TestCase("1234567890abcdef12345678")]
        public void NoDeviceTypesInDatabase_DeviceTypeGet_ReturnsNotFound(string id)
        {
            var ex = Assert.ThrowsAsync<BeimaException>(async () =>
                await TestClient.GetDeviceType(id)
            );
            Assert.IsNotNull(ex);
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [TestCase("xxx")]
        [TestCase("1234")]
        [TestCase("1234567890abcdef1234567x")]
        [TestCase("1234567890abcdef12345678")]
        public void InvalidCredentials_DeviceTypeGet_ReturnsUnauthenticated(string id)
        {
            // ARRANGE
            // ACT
            var ex = Assert.ThrowsAsync<BeimaException>(async () =>
                await UnauthorizedTestClient.GetDeviceType(id)
            );

            // ASSERT
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex?.Message, Does.Contain("Invalid credentials."));
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        #endregion Device Type GET Tests

        #region Device Type Add Tests

        [Test]
        public async Task NoDeviceTypesInDb_AddDeviceType_DeviceTypeAddedSuccessfullyAsync()
        {
            // ARRANGE
            var device = new DeviceTypeAdd
            {
                Description = "This is a boiler type.",
                Name = "Boiler",
                Notes = "Some type notes.",
                Fields = new List<string>()
                {
                    "MaxTemperature",
                    "MinTemperature",
                    "Capacity"
                }
            };

            // ACT
            var responseId = await TestClient.AddDeviceType(device);

            // ASSERT
            Assert.That(responseId, Is.Not.Null);
            Assert.That(ObjectId.TryParse(responseId, out _), Is.True);
            var getDeviceType = await TestClient.GetDeviceType(responseId);
            Assert.That(getDeviceType.Name, Is.EqualTo(device.Name));
            Assert.That(getDeviceType.Notes, Is.EqualTo(device.Notes));
            Assert.That(getDeviceType.Description, Is.EqualTo(device.Description));
            Assert.That(getDeviceType.Count, Is.EqualTo(0));
            Assert.That(getDeviceType.Fields, Is.Not.Null.And.Not.Empty);
            Assert.That(getDeviceType.Fields?.Count, Is.EqualTo(device.Fields.Count));
            if (getDeviceType.Fields != null)
            {
                foreach (var key in getDeviceType.Fields.Keys)
                {
                    Assert.That(Guid.TryParse(key, out _), Is.True);
                }
            }
            Assert.That(getDeviceType.Fields, Contains.Value(device.Fields[0]));
            Assert.That(getDeviceType.Fields, Contains.Value(device.Fields[1]));
            Assert.That(getDeviceType.Fields, Contains.Value(device.Fields[2]));
        }

        [Test]
        public void InvalidCredentials_AddDeviceType_ReturnsUnauthorized()
        {
            // ARRANGE
            var device = new DeviceTypeAdd
            {
                Description = "This is a boiler type.",
                Name = "Boiler",
                Notes = "Some type notes.",
                Fields = new List<string>()
                {
                    "MaxTemperature",
                    "MinTemperature",
                    "Capacity"
                }
            };

            // ACT
            var ex = Assert.ThrowsAsync<BeimaException>(async () =>
                await UnauthorizedTestClient.AddDeviceType(device)
            );

            // ASSERT
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex?.Message, Does.Contain("Invalid credentials."));
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        #endregion Device Type Add Tests

        #region Device Type List Tests

        [Test]
        public async Task DeviceTypesInDatabase_GetDeviceTypeList_ReturnsDeviceTypeList()
        {
            // ARRANGE
            var deviceTypeList = new List<DeviceTypeAdd>
            {
                new DeviceTypeAdd
                {
                    Description = "Boiler description.",
                    Name = "Boiler",
                    Notes = "Some boiler notes.",
                    Fields = new List<string>
                    {
                        "Type",
                        "Fuel Input Rate",
                        "Output",
                    },
                },
                new DeviceTypeAdd
                {
                    Description = "Meters description",
                    Name = "Electric Meters",
                    Notes = "Some meter notes.",
                    Fields = new List<string>
                    {
                        "Account Number",
                        "Service ID",
                        "Voltage",
                    },
                },
                new DeviceTypeAdd
                {
                    Description = "Cooling tower description",
                    Name = "Cooling Tower",
                    Notes = "Some cooling tower notes.",
                    Fields = new List<string>
                    {
                        "Type",
                        "Chiller(s) Served",
                        "Capacity",
                    },
                },
            };

            foreach (var deviceType in deviceTypeList)
            {
                await TestClient.AddDeviceType(deviceType);
            }

            // ACT
            var actualDeviceTypes = await TestClient.GetDeviceTypeList();

            // ASSERT
            Assert.That(actualDeviceTypes.Count, Is.EqualTo(3));

            foreach (var deviceType in actualDeviceTypes)
            {
                Assert.That(deviceType, Is.Not.Null);
                var expectedDeviceType = deviceTypeList.Single(dt => dt.Name?.Equals(deviceType.Name) ?? false);

                Assert.That(deviceType.Description, Is.EqualTo(expectedDeviceType.Description));
                Assert.That(deviceType.Name, Is.EqualTo(expectedDeviceType.Name));
                Assert.That(deviceType.Notes, Is.EqualTo(expectedDeviceType.Notes));
                Assert.That(deviceType.Count, Is.EqualTo(0));

                Assert.That(deviceType.Fields, Is.Not.Null.And.Not.Empty);
                if (deviceType.Fields != null)
                {
                    foreach (var field in deviceType.Fields)
                    {
                        Assert.That(Guid.TryParse(field.Key, out _), Is.True);
                    }
                }

                Assume.That(expectedDeviceType.Fields, Is.Not.Null.And.Not.Empty);
                if (expectedDeviceType.Fields != null)
                {
                    foreach (var field in expectedDeviceType.Fields)
                    {
                        Assert.That(deviceType.Fields, Contains.Value(field));
                    }
                }

                Assert.That(deviceType.LastModified, Is.Not.Null);
                Assert.That(deviceType.LastModified?.Date, Is.Not.Null);
                Assert.That(deviceType.LastModified?.User, Is.EqualTo(TestUsername));
            }
        }

        [Test]
        public async Task InvalidCredentials_GetDeviceTypeList_ReturnsUnauthenticated()
        {
            // ARRANGE
            var deviceTypeList = new List<DeviceTypeAdd>
            {
                new DeviceTypeAdd
                {
                    Description = "Boiler description.",
                    Name = "Boiler",
                    Notes = "Some boiler notes.",
                    Fields = new List<string>
                    {
                        "Type",
                        "Fuel Input Rate",
                        "Output",
                    },
                },
                new DeviceTypeAdd
                {
                    Description = "Meters description",
                    Name = "Electric Meters",
                    Notes = "Some meter notes.",
                    Fields = new List<string>
                    {
                        "Account Number",
                        "Service ID",
                        "Voltage",
                    },
                },
                new DeviceTypeAdd
                {
                    Description = "Cooling tower description",
                    Name = "Cooling Tower",
                    Notes = "Some cooling tower notes.",
                    Fields = new List<string>
                    {
                        "Type",
                        "Chiller(s) Served",
                        "Capacity",
                    },
                },
            };

            foreach (var deviceType in deviceTypeList)
            {
                await TestClient.AddDeviceType(deviceType);
            }

            // ACT
            var ex = Assert.ThrowsAsync<BeimaException>(async () =>
                await UnauthorizedTestClient.GetDeviceTypeList()
            );

            // ASSERT
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex?.Message, Does.Contain("Invalid credentials."));
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        #endregion Device Type List Tests

        #region Device Type Delete Tests

        [Test]
        public async Task DeviceTypeInDatabase_DeleteDeviceType_DeviceTypeDeletedSuccessfully()
        {
            // ARRANGE
            var deviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some other boiler notes.",
                Fields = new List<string>
                    {
                        "Type",
                        "Fuel Input Rate",
                        "Output",
                    },
            };

            var deviceTypeId = await TestClient.AddDeviceType(deviceType);
            Assume.That(await TestClient.GetDeviceType(deviceTypeId), Is.Not.Null);

            // ACT
            Assert.DoesNotThrowAsync(async () => await TestClient.DeleteDeviceType(deviceTypeId));

            // ASSERT
            var ex = Assert.ThrowsAsync<BeimaException>(async () => await TestClient.GetDeviceType(deviceTypeId));
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task DeviceTypeWithDeviceInDatabase_DeleteDeviceType_DeletetionStopped()
        {
            // ARRANGE
            var deviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some other boiler notes.",
                Fields = new List<string>
                    {
                        "Type",
                        "Fuel Input Rate",
                        "Output",
                    },
            };

            var deviceTypeId = await TestClient.AddDeviceType(deviceType);
            var addedDeviceType = await TestClient.GetDeviceType(deviceTypeId);

            var device = new Device()
            {
                DeviceTag = "B-34",
                DeviceTypeId = deviceTypeId,
                Fields = new Dictionary<string, string> {
                    { addedDeviceType.Fields?.Keys?.ToList()[0] ?? "Bad Fields", "Tall boiler." },
                    { addedDeviceType.Fields?.Keys?.ToList()[1] ?? "Bad Fields", "23" },
                    { addedDeviceType.Fields?.Keys?.ToList()[2] ?? "Bad Fields", "38" },
                },
                Location = new DeviceLocation()
                {
                    BuildingId = null,
                    Latitude = "78.6",
                    Longitude = "43.2",
                    Notes = "Some notes."
                },
                Manufacturer = "Generic Inc.",
                ModelNum = "1234",
                Notes = "Some notes.",
                SerialNum = "4321",
                YearManufactured = 2001,
            };

            var deviceId = await TestClient.AddDevice(device);
            Assume.That(deviceId, Is.Not.Null.And.Not.Empty);

            // ACT
            var ex = Assert.ThrowsAsync<BeimaException>(async () => await TestClient.DeleteDeviceType(deviceTypeId));

            // ASSERT
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(ex?.Message,
                        Contains.Substring("The device type could not be deleted because at least one device exists in the database with this device type."));
        }

        [Test]
        public async Task InvalidCredentials_DeleteDeviceType_ReturnsUnauthenticated()
        {
            // ARRANGE
            var deviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some other boiler notes.",
                Fields = new List<string>
                    {
                        "Type",
                        "Fuel Input Rate",
                        "Output",
                    },
            };

            var deviceTypeId = await TestClient.AddDeviceType(deviceType);
            Assume.That(await TestClient.GetDeviceType(deviceTypeId), Is.Not.Null);

            // ACT
            var ex = Assert.ThrowsAsync<BeimaException>(async () =>
                await UnauthorizedTestClient.DeleteDeviceType(deviceTypeId)
            );

            // ASSERT
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex?.Message, Does.Contain("Invalid credentials."));
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        #endregion Device Type Delete Tests

        #region Device Type Update Tests

        [Test]
        public async Task DeviceTypeInDatabase_UpdateDeviceType_ReturnsUpdatedDeviceType()
        {
            // ARRANGE
            var origDeviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some boiler notes.",
                Fields = new List<string>
                {
                    "Type",
                    "Fuel Input Rate",
                    "Output",
                },
            };

            var deviceTypeId = await TestClient.AddDeviceType(origDeviceType);
            var origItem = await TestClient.GetDeviceType(deviceTypeId);

            var updateDictionary = new Dictionary<string, string>();
            if (origItem.Fields != null)
            {
                foreach (var item in origItem.Fields)
                {
                    updateDictionary.Add(item.Key, item.Value);
                    if (item.Value.Equals("Type"))
                    {
                        updateDictionary[item.Key] = "Boiler Type";
                    }
                }
            }

            var updateItem = new DeviceTypeUpdate
            {
                Id = origItem.Id,
                Description = origItem.Description,
                Name = origItem.Name,
                Notes = "Some other boiler notes.",
                Fields = updateDictionary,
                NewFields = new List<string>
                {
                    "Capacity",
                },
            };

            // ACT
            var updatedDeviceType = await TestClient.UpdateDeviceType(updateItem);

            // ASSERT
            Assert.That(updatedDeviceType, Is.Not.Null);
            Assert.That(updatedDeviceType.Notes, Is.Not.EqualTo(origDeviceType.Notes));

            Assert.That(updatedDeviceType.LastModified?.Date, Is.Not.EqualTo(origItem.LastModified?.Date));
            Assert.That(updatedDeviceType.LastModified?.User, Is.EqualTo(origItem.LastModified?.User));

            Assert.That(updatedDeviceType.Id, Is.EqualTo(updateItem.Id));
            Assert.That(updatedDeviceType.Description, Is.EqualTo(updateItem.Description));
            Assert.That(updatedDeviceType.Name, Is.EqualTo(updateItem.Name));
            Assert.That(updatedDeviceType.Notes, Is.EqualTo(updateItem.Notes));

            Assume.That(origItem, Is.Not.Null);
            foreach (var item in updateItem.Fields)
            {
                Assert.That(updatedDeviceType.Fields, Contains.Key(item.Key));
                Assert.That(updatedDeviceType.Fields?[item.Key], Is.EqualTo(item.Value));
            }
            Assert.That(updatedDeviceType.Fields?.Where(kv => kv.Value.Equals("Type")), Is.Empty);
            Assert.That(Guid.TryParse(updatedDeviceType.Fields?.Single(kv => kv.Value.Equals("Capacity")).Key, out _), Is.True);
        }

        [Test]
        public async Task DeviceAndDeviceTypeInDatabase_UpdateDeviceType_ReturnsUpdatedDeviceAndDeviceType()
        {
            // ARRANGE
            var origDeviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some boiler notes.",
                Fields = new List<string>
                {
                    "Boiler Type",
                    "Fuel Input Rate",
                    "Output",
                },
            };

            var deviceTypeId = await TestClient.AddDeviceType(origDeviceType);
            var origItem = await TestClient.GetDeviceType(deviceTypeId);

            var origDevice = new Device()
            {
                DeviceTag = "B-34",
                DeviceTypeId = deviceTypeId,
                Fields = new Dictionary<string, string> {
                    { origItem.Fields?.Keys?.ToList()[0] ?? "Bad Fields", "Tall boiler." },
                    { origItem.Fields?.Keys?.ToList()[1] ?? "Bad Fields", "23" },
                    { origItem.Fields?.Keys?.ToList()[2] ?? "Bad Fields", "38" },
                },
                Location = new DeviceLocation()
                {
                    BuildingId = null,
                    Latitude = "78.6",
                    Longitude = "43.2",
                    Notes = "Some notes."
                },
                Manufacturer = "Generic Inc.",
                ModelNum = "1234",
                Notes = "Some notes.",
                SerialNum = "4321",
                YearManufactured = 2001,
            };

            var updateDictionary = new Dictionary<string, string>();
            if (origItem.Fields != null)
            {
                foreach (var item in origItem.Fields)
                {
                    if (!item.Value.Equals("Boiler Type"))
                    {
                        updateDictionary.Add(item.Key, item.Value);
                    }
                }
            }

            var deviceId = await TestClient.AddDevice(origDevice);

            var updateItem = new DeviceTypeUpdate
            {
                Id = origItem.Id,
                Description = origItem.Description,
                Name = origItem.Name,
                Notes = "Some other boiler notes.",
                Fields = updateDictionary,
                NewFields = new List<string>
                {
                    "Capacity",
                },
            };

            // ACT
            var updatedDeviceType = await TestClient.UpdateDeviceType(updateItem);

            // ASSERT
            Assert.That(updatedDeviceType, Is.Not.Null);
            Assert.That(updatedDeviceType.Notes, Is.Not.EqualTo(origDeviceType.Notes));

            Assert.That(updatedDeviceType.LastModified?.Date, Is.Not.EqualTo(origItem.LastModified?.Date));
            Assert.That(updatedDeviceType.LastModified?.User, Is.EqualTo(origItem.LastModified?.User));

            Assert.That(updatedDeviceType.Id, Is.EqualTo(updateItem.Id));
            Assert.That(updatedDeviceType.Description, Is.EqualTo(updateItem.Description));
            Assert.That(updatedDeviceType.Name, Is.EqualTo(updateItem.Name));
            Assert.That(updatedDeviceType.Notes, Is.EqualTo(updateItem.Notes));

            Assume.That(origItem, Is.Not.Null);
            foreach (var item in updateItem.Fields)
            {
                Assert.That(updatedDeviceType.Fields, Contains.Key(item.Key));
                Assert.That(updatedDeviceType.Fields?[item.Key], Is.EqualTo(item.Value));
            }
            Assert.That(updatedDeviceType.Fields?.Where(kv => kv.Value.Equals("Type")), Is.Empty);
            Assert.That(Guid.TryParse(updatedDeviceType.Fields?.Single(kv => kv.Value.Equals("Capacity")).Key, out _), Is.True);

            var device = await TestClient.GetDevice(deviceId);
            foreach (var key in device.Fields?.Keys.ToList() ?? new List<string>())
            {
                Assert.That(updatedDeviceType.Fields, Contains.Key(key));
            }
        }

        [Test]
        public async Task InvalidCredentials_UpdateDeviceType_ReturnsUnauthorized()
        {
            // ARRANGE
            var origDeviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some boiler notes.",
                Fields = new List<string>
                {
                    "Type",
                    "Fuel Input Rate",
                    "Output",
                },
            };

            var deviceTypeId = await TestClient.AddDeviceType(origDeviceType);
            var origItem = await TestClient.GetDeviceType(deviceTypeId);

            
            var updateDictionary = new Dictionary<string, string>();
            if (origItem.Fields != null)
            {
                foreach (var item in origItem.Fields)
                {
                    updateDictionary.Add(item.Key, item.Value);
                    if (item.Value.Equals("Type"))
                    {
                        updateDictionary[item.Key] = "Boiler Type";
                    }
                }
            }

            var updateItem = new DeviceTypeUpdate
            {
                Id = origItem.Id,
                Description = origItem.Description,
                Name = origItem.Name,
                Notes = "Some other boiler notes.",
                Fields = updateDictionary,
                NewFields = new List<string>
                {
                    "Capacity",
                },
            };

            // ACT
            var ex = Assert.ThrowsAsync<BeimaException>(async () =>
                await UnauthorizedTestClient.UpdateDeviceType(updateItem)
            );

            // ASSERT
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex?.Message, Does.Contain("Invalid credentials."));
            Assert.That(ex?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
        
        public async Task DeviceAndDeviceTypeInDatabase_GetDeviceType_ReturnsDeviceTypeWithCount()
        {
            // ARRANGE
            var origDeviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some boiler notes.",
                Fields = new List<string>
                {
                    "Boiler Type",
                    "Fuel Input Rate",
                    "Output",
                },
            };

            var deviceTypeId = await TestClient.AddDeviceType(origDeviceType);
            var origItem = await TestClient.GetDeviceType(deviceTypeId);


       
            var origDevice = new Device()
            {
                DeviceTag = "B-34",
                DeviceTypeId = deviceTypeId,
                Fields = new Dictionary<string, string> {
                    { origItem.Fields?.Keys?.ToList()[0] ?? "Bad Fields", "Tall boiler." },
                    { origItem.Fields?.Keys?.ToList()[1] ?? "Bad Fields", "23" },
                    { origItem.Fields?.Keys?.ToList()[2] ?? "Bad Fields", "38" },
                },
                Location = new DeviceLocation()
                {
                    BuildingId = null,
                    Latitude = "78.6",
                    Longitude = "43.2",
                    Notes = "Some notes."
                },
                Manufacturer = "Generic Inc.",
                ModelNum = "1234",
                Notes = "Some notes.",
                SerialNum = "4321",
                YearManufactured = 2001,
            };

            var deviceId = await TestClient.AddDevice(origDevice);

            // ACT
            var deviceType = await TestClient.GetDeviceType(deviceTypeId);

            // ASSERT
            Assert.That(deviceType.Name, Is.EqualTo(origDeviceType.Name));
            Assert.That(deviceType.Notes, Is.EqualTo(origDeviceType.Notes));
            Assert.That(deviceType.Description, Is.EqualTo(origDeviceType.Description));
            Assert.That(deviceType.Count, Is.EqualTo(1));
            Assert.That(deviceType.Fields, Is.Not.Null.And.Not.Empty);
            Assert.That(deviceType.Fields?.Count, Is.EqualTo(origDeviceType.Fields.Count));
            if (deviceType.Fields != null)
            {
                foreach (var key in deviceType.Fields.Keys)
                {
                    Assert.That(Guid.TryParse(key, out _), Is.True);
                }
            }
            Assert.That(deviceType.Fields, Contains.Value(origDeviceType.Fields[0]));
            Assert.That(deviceType.Fields, Contains.Value(origDeviceType.Fields[1]));
            Assert.That(deviceType.Fields, Contains.Value(origDeviceType.Fields[2]));
        }


        [Test]
        public async Task DeviceAndDeviceTypeInDatabase_GetDeviceTypeList_ReturnsDeviceTypeWithCount()
        {
            // ARRANGE
            var origDeviceType = new DeviceTypeAdd
            {
                Description = "Boiler description.",
                Name = "Boiler",
                Notes = "Some boiler notes.",
                Fields = new List<string>
                {
                    "Boiler Type",
                    "Fuel Input Rate",
                    "Output",
                },
            };

            var deviceTypeId = await TestClient.AddDeviceType(origDeviceType);
            var origItem = await TestClient.GetDeviceType(deviceTypeId);

            var origDevice = new Device()
            {
                DeviceTag = "B-34",
                DeviceTypeId = deviceTypeId,
                Fields = new Dictionary<string, string> {
                    { origItem.Fields?.Keys?.ToList()[0] ?? "Bad Fields", "Tall boiler." },
                    { origItem.Fields?.Keys?.ToList()[1] ?? "Bad Fields", "23" },
                    { origItem.Fields?.Keys?.ToList()[2] ?? "Bad Fields", "38" },
                },
                Location = new DeviceLocation()
                {
                    BuildingId = null,
                    Latitude = "78.6",
                    Longitude = "43.2",
                    Notes = "Some notes."
                },
                Manufacturer = "Generic Inc.",
                ModelNum = "1234",
                Notes = "Some notes.",
                SerialNum = "4321",
                YearManufactured = 2001,
            };

            await TestClient.AddDevice(origDevice);

            // ACT
            var deviceTypeList = await TestClient.GetDeviceTypeList();

            // ASSERT
            Assert.That(deviceTypeList.Count, Is.EqualTo(1));
            foreach(var deviceType in deviceTypeList)
            {
                Assert.That(deviceType.Name, Is.EqualTo(origDeviceType.Name));
                Assert.That(deviceType.Notes, Is.EqualTo(origDeviceType.Notes));
                Assert.That(deviceType.Description, Is.EqualTo(origDeviceType.Description));
                Assert.That(deviceType.Count, Is.EqualTo(1));
                Assert.That(deviceType.Fields, Is.Not.Null.And.Not.Empty);
                Assert.That(deviceType.Fields?.Count, Is.EqualTo(origDeviceType.Fields.Count));
                if (deviceType.Fields != null)
                {
                    foreach (var key in deviceType.Fields.Keys)
                    {
                        Assert.That(Guid.TryParse(key, out _), Is.True);
                    }
                }
                Assert.That(deviceType.Fields, Contains.Value(origDeviceType.Fields[0]));
                Assert.That(deviceType.Fields, Contains.Value(origDeviceType.Fields[1]));
                Assert.That(deviceType.Fields, Contains.Value(origDeviceType.Fields[2]));
            }
        }
    } 
    #endregion Device Type Update Tests
}
