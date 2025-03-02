﻿using BEIMA.Backend.UserFunctions;
using BEIMA.Backend.MongoService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using static BEIMA.Backend.Test.RequestFactory;
using BEIMA.Backend.AuthService;
using Microsoft.AspNetCore.Http;
using BEIMA.Backend.Models;
using System.Net;

namespace BEIMA.Backend.Test.UserFunctions
{
    [TestFixture]
    public class GetUserListTest : UnitTestBase
    {
        [Test]
        public void PopulatedDatabase_GetUserList_ReturnsListOfUsers()
        {
            // ARRANGE
            var user1 = new User(ObjectId.GenerateNewId(), "user.name1", "password1!", "FirstName1", "LastName1", "user");
            var user2 = new User(ObjectId.GenerateNewId(), "user.name2", "password2!", "FirstName2", "LastName2", "user");
            var user3 = new User(ObjectId.GenerateNewId(), "user.name3", "password3!", "FirstName3", "LastName3", "user");

            user1.SetLastModified(DateTime.UtcNow, "Anonymous");
            user2.SetLastModified(DateTime.UtcNow, "Anonymous");
            user3.SetLastModified(DateTime.UtcNow, "Anonymous");

            var userList = new List<BsonDocument>
            {
                user1.GetBsonDocument(),
                user2.GetBsonDocument(),
                user3.GetBsonDocument(),
            };
            Mock<IMongoConnector> mockDb = new Mock<IMongoConnector>();
            mockDb.Setup(mock => mock.GetAllUsers())
                  .Returns(userList)
                  .Verifiable();
            MongoDefinition.MongoInstance = mockDb.Object;

            Mock<IAuthenticationService> mockAuth = new Mock<IAuthenticationService>();
            mockAuth.Setup(mock => mock.ParseToken(It.IsAny<HttpRequest>()))
                .Returns(new Claims { Role = Constants.ADMIN_ROLE, Username = "Bob" })
                .Verifiable();
            AuthenticationDefinition.AuthenticationInstance = mockAuth.Object;

            var request = CreateHttpRequest(RequestMethod.GET);
            var logger = (new LoggerFactory()).CreateLogger("Testing");

            // ACT
            var response = (OkObjectResult)GetUserList.Run(request, logger);

            // ASSERT
            Assert.DoesNotThrow(() => mockAuth.Verify(mock => mock.ParseToken(It.IsAny<HttpRequest>()), Times.Once));
            Assert.DoesNotThrow(() => mockDb.Verify(mock => mock.GetAllUsers(), Times.Once));

            var getList = (List<User>)response.Value;
            Assert.IsNotNull(getList);
            for (int i = 0; i < userList.Count; i++)
            {
                var user = getList[i];
                var expectedUser = userList[i];
                Assert.That(user.Id.ToString(), Is.EqualTo(expectedUser["_id"].AsObjectId.ToString()));
                Assert.That(user.Username, Is.EqualTo(expectedUser["username"].AsString));
                // GET endpoints should always return empty string
                Assert.That(user.Password, Is.EqualTo(string.Empty));
                Assert.That(user.FirstName, Is.EqualTo(expectedUser["firstName"].AsString));
                Assert.That(user.LastName, Is.EqualTo(expectedUser["lastName"].AsString));
                Assert.That(user.Role, Is.EqualTo(expectedUser["role"].AsString));

                var lastMod = user.LastModified;
                var expectedLastMod = expectedUser["lastModified"];
                Assert.That(lastMod.Date, Is.EqualTo(expectedLastMod["date"].ToUniversalTime()));
                Assert.That(lastMod.User, Is.EqualTo(expectedLastMod["user"].AsString));
            }
        }

        [TestCaseSource(nameof(ClaimsFactory))]
        public void InvalidCredentials_GetUserList_ReturnsUnauthorized(Claims claim)
        {
            // ARRANGE
            Mock<IMongoConnector> mockDb = new Mock<IMongoConnector>();
            MongoDefinition.MongoInstance = mockDb.Object;

            Mock<IAuthenticationService> mockAuth = new Mock<IAuthenticationService>();
            mockAuth.Setup(mock => mock.ParseToken(It.IsAny<HttpRequest>()))
                .Returns(claim)
                .Verifiable();
            AuthenticationDefinition.AuthenticationInstance = mockAuth.Object;

            var request = CreateHttpRequest(RequestMethod.GET);
            var logger = (new LoggerFactory()).CreateLogger("Testing");

            // ACT
            var response = (ObjectResult)GetUserList.Run(request, logger);

            // ASSERT
            Assert.DoesNotThrow(() => mockAuth.Verify(mock => mock.ParseToken(It.IsAny<HttpRequest>()), Times.Once));
            Assert.DoesNotThrow(() => mockDb.Verify(mock => mock.GetAllUsers(), Times.Never));

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(response.Value, Is.EqualTo("Invalid credentials."));
        }

        private static IEnumerable<Claims?> ClaimsFactory()
        {
            yield return null;
            yield return new Claims { Role = "nonadmin", Username = "Bob" };
        }
    }
}