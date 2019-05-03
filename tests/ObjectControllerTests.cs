using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core;
using Mongo2Go;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Foundation.ObjectService.Data;
using Foundation.ObjectService.WebUI.Controllers;
using Foundation.ObjectService.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Foundation.ObjectService.WebUI.Tests
{
    public partial class ObjectControllerTests : IClassFixture<ObjectControllerFixture>
    {
        ObjectControllerFixture _fixture;

        private const string DATABASE_NAME = "bookstore";
        private const string BOOKS_COLLECTION_NAME = "books";

        public ObjectControllerTests(ObjectControllerFixture fixture)
        {
            this._fixture = fixture;
        }

        #region Single object get
        [Theory]
        [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }", "{ \"_id\" : \"1\", \"title\" : \"The Red Badge of Courage\" }")]
        [InlineData("2", "{ \"title\": \"Don Quixote\" }", "{ \"_id\" : \"2\", \"title\" : \"Don Quixote\" }")]
        [InlineData("3", "{ \"title\": \"A Connecticut Yankee in King Arthur's Court\" }", "{ \"_id\" : \"3\", \"title\" : \"A Connecticut Yankee in King Arthur's Court\" }")]
        public async Task Get_Object_by_Primitive_Id(string id, string insertedJson, string expectedJson)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "books1";

            // Act
            var insertResult = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, insertedJson, ResponseFormat.OnlyId);

            var getResult = await controller.GetObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id });

            OkObjectResult okResult = ((OkObjectResult)getResult);
            string receivedJson = okResult.Value.ToString();

            // Assert
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedJson, receivedJson);
        }

        [Theory]
        [InlineData("{ \"title\": \"The Red Badge of Courage\" }", "{ \"_id\" : REPLACEME, \"title\" : \"The Red Badge of Courage\" }")]
        [InlineData("{ \"title\": \"Don Quixote\" }", "{ \"_id\" : REPLACEME, \"title\" : \"Don Quixote\" }")]
        [InlineData("{ \"title\": \"A Connecticut Yankee in King Arthur's Court\" }", "{ \"_id\" : REPLACEME, \"title\" : \"A Connecticut Yankee in King Arthur's Court\" }")]
        public async Task Get_Object_by_ObjectId(string insertedJson, string expectedJson)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "books2";

            // Act
            var insertResult = await controller.InsertObjectWithNoId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, insertedJson, ResponseFormat.OnlyId);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)insertResult);
            JObject jsonObject = JObject.Parse(createdResult.Value.ToString());

            JArray jsonArray = JArray.Parse(jsonObject["ids"].ToString());
            var id = jsonArray[0].ToString();

            var getResult = await controller.GetObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id });

            OkObjectResult okResult = ((OkObjectResult)getResult);
            string receivedJson = okResult.Value.ToString();

            // Assert
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedJson.Replace("REPLACEME", "{ \"$oid\" : \"" + id + "\" }"), receivedJson);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("ABCD")]
        public async Task Get_Object_fail_Not_Found(string id)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "books3";

            var getResult = await controller.GetObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id });
            ObjectResult notFoundResult = ((ObjectResult)getResult);

            // Assert
            Assert.Equal(404, notFoundResult.StatusCode);
        }
        #endregion // Single object get

        #region Single object insertions
        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }", "A")]
        [InlineData("2", "{ \"name\": \"AB\", \"fullname\": { first: \"A\", last: \"B\" } }", "AB")]
        [InlineData("3", "{ \"name\": \"A\", status: 5643 }", "A")]
        [InlineData("4", "{ \"name\": \"A\", status: 5643, \"events\": [ 1, 2, 3, 4 ] }", "A")]
        [InlineData("5", "{ \"name\": \"A\", status: 0, \"events\": [ { \"id\": 5 }, { \"id\": 6 } ] }", "A")]
        [InlineData("6", "{ \"name\": 'C' }", "C")]
        [InlineData("7", "{ 'name': 'D' }", "D")]
        [InlineData("8", "{ name: 'E' }", "E")]
        // The service should insert Json objects with specified IDs
        public async Task Insert_with_Primitive_Id_Full_Response(string id, string json, string expectedName)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act
            var result = await controller.InsertObjectWithId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "logs1",
                Id = id
            }, json, ResponseFormat.EntireObject);

            var expected = typeof(CreatedAtActionResult);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)result);

            JObject jsonObject = JObject.Parse(createdResult.Value.ToString());
            
            var insertedId = jsonObject["_id"].ToString();
            var insertedName = jsonObject["name"].ToString();

            // Assert
            Assert.IsType(expected, result);
            Assert.Equal(id, insertedId);
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(201, createdResult.StatusCode);
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }")]
        [InlineData("7", "{ 'name': 'D' }")]
        // The service should insert Json objects with specified ID, and if asked to by the client, only return the IDs to the client instead of the entire Json object
        public async Task Insert_with_Primitive_Id_Sparse_Response(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act
            var result = await controller.InsertObjectWithId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "logs2",
                Id = id
            }, json, ResponseFormat.OnlyId);

            var expected = typeof(CreatedAtActionResult);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)result);

            JObject jsonObject = JObject.Parse(createdResult.Value.ToString());
            
            var count = jsonObject["inserted"].ToString();

            JArray jsonArray = JArray.Parse(jsonObject["ids"].ToString());
            var insertedId = jsonArray[0].ToString();

            // Assert
            Assert.IsType(expected, result);
            Assert.Equal(id, insertedId);
            Assert.Equal(201, createdResult.StatusCode);
        }

        [Theory]
        [InlineData("{ \"name\": \"A\" }", "A")]
        [InlineData("{ \"name\": \"AB\", \"fullname\": { first: \"A\", last: \"B\" } }", "AB")]
        [InlineData("{ \"name\": \"A\", status: 5643 }", "A")]
        [InlineData("{ \"name\": \"A\", status: 5643, \"events\": [ 1, 2, 3, 4 ] }", "A")]
        [InlineData("{ \"name\": \"A\", status: 0, \"events\": [ { \"id\": 5 }, { \"id\": 6 } ] }", "A")]
        [InlineData("{ \"name\": 'C' }", "C")]
        [InlineData("{ 'name': 'D' }", "D")]
        [InlineData("{ name: 'E' }", "E")]
        // The service should insert Json objects when there is no specified Id, and it should create Ids for those records automatically
        public async Task Insert_with_no_Id_Full_Response(string json, string expectedName)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act
            var result = await controller.InsertObjectWithNoId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "logs3"
            }, json, ResponseFormat.EntireObject);

            var expected = typeof(CreatedAtActionResult);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)result);

            JObject jsonObject = JObject.Parse(createdResult.Value.ToString());
            
            var id = jsonObject.SelectTokens(@"_id.$oid").FirstOrDefault().Value<string>();
            var insertedName = jsonObject["name"].ToString();

            // Assert
            Assert.IsType(expected, result);
            Assert.True(ObjectId.TryParse(id, out _));
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(201, createdResult.StatusCode);
        }

        [Theory]
        [InlineData("{ \"name\": \"A\" }")]
        [InlineData("{ 'name': 'D' }")]
        // The service should insert Json objects when there is no specified Id, and it should create Ids for those records automaticallyand if asked to by the client, only return the IDs to the client instead of the entire Json object
        public async Task Insert_with_no_Id_Sparse_Response(string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act
            var result = await controller.InsertObjectWithNoId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "logs4"
            }, json, ResponseFormat.OnlyId);

            var expected = typeof(CreatedAtActionResult);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)result);

            JObject jsonObject = JObject.Parse(createdResult.Value.ToString());
            
            var count = jsonObject["inserted"].ToString();

            JArray jsonArray = JArray.Parse(jsonObject["ids"].ToString());
            var insertedId = jsonArray[0].ToString();

            // Assert
            Assert.IsType(expected, result);
            Assert.True(ObjectId.TryParse(insertedId, out _));
            Assert.Equal(201, createdResult.StatusCode);
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }")]
        [InlineData("XQP", "{ \"name\": \"A\" }")]
        // Disallow inserting an object where an object already exists with that ID
        public async Task Insert_fails_Duplicate_Ids(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            string collectionName = "logs5";

            // Act - first insert
            var result1 = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, json, ResponseFormat.OnlyId);

            var expected1 = typeof(CreatedAtActionResult);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)result1);

            // Assert first insert was a success
            Assert.IsType(expected1, result1);
            Assert.Equal(201, createdResult.StatusCode);

            // Act - second insert
            try 
            {
                var result2 = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, json, ResponseFormat.OnlyId);
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                Assert.IsType<MongoDB.Driver.MongoWriteException>(ex);
            }
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" ")]
        [InlineData("2", " \"name\": \"A\" }")]
        [InlineData("3", " \"name\": \"A\" ")]
        [InlineData("4", " ")]
        // Disallow inserting an object with malformed Json
        public async Task Insert_fails_Malformed_Json(string id, string json)
        {
            var controller = new ObjectController(_fixture.MongoRepository);
            string collectionName = "logs6";

            try 
            {
                var result = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, json, ResponseFormat.OnlyId);
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                Assert.IsType<System.FormatException>(ex);
            }
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }")]
        // Disallow inserting an object to an immutable collection
        public async Task Insert_fails_Immutable_Collection(string id, string json)
        {
            var controller = new ObjectController(_fixture.MongoRepository);
            try 
            {
                var result = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = "immutabledatabase", CollectionName = "immutablecollection", Id = id }, json, ResponseFormat.OnlyId);
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                Assert.IsType<Foundation.ObjectService.Exceptions.ImmutableCollectionException>(ex);
            }
        }

        #endregion // Single object insertions

        #region Single object replacements
        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }", "A")]
        [InlineData("6", "{ \"name\": 'C' }", "C")]
        [InlineData("7", "{ 'name': 'D' }", "D")]
        [InlineData("8", "{ name: 'E' }", "E")]
        // The service should replace Json objects with specified IDs
        public async Task Replace_with_Primitive_Id_Full_Response(string id, string json, string expectedName)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act - insert first
            var insertResult = await controller.InsertObjectWithId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers1",
                Id = id
            }, json, ResponseFormat.EntireObject);

            var expectedInsert = typeof(CreatedAtActionResult);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)insertResult);

            // Assert
            Assert.Equal(201, createdResult.StatusCode);

            // Act - do the replacement
            var replaceResult = await controller.ReplaceObject(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers1",
                Id = id
            }, json, ResponseFormat.EntireObject);

            var expectedReplace = typeof(OkObjectResult);
            OkObjectResult okResult = ((OkObjectResult)replaceResult);

            JObject jsonObject = JObject.Parse(okResult.Value.ToString());
            
            var insertedId = jsonObject["_id"].ToString();
            var insertedName = jsonObject["name"].ToString();

            // Assert
            Assert.IsType(expectedReplace, okResult);
            Assert.Equal(id, insertedId);
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Theory]
        [InlineData("{ \"name\": \"A\" }", "A")]
        [InlineData("{ \"name\": 'C' }", "C")]
        // The service should replace Json objects with specified IDs when using the more complex ObjectId format
        public async Task Replace_with_ObjectId_Full_Response(string json, string expectedName)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act - insert first
            var insertResult = await controller.InsertObjectWithId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers9",
            }, json, ResponseFormat.EntireObject);

            var expectedInsert = typeof(CreatedAtActionResult);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)insertResult);

            // Assert
            Assert.Equal(201, createdResult.StatusCode);

            JObject jsonObject1 = JObject.Parse(createdResult.Value.ToString());

            var id = jsonObject1.SelectTokens(@"_id.$oid").FirstOrDefault().Value<string>();

            // Act - do the replacement
            var replaceResult = await controller.ReplaceObject(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers9",
                Id = id
            }, json, ResponseFormat.EntireObject);

            var expectedReplace = typeof(OkObjectResult);
            OkObjectResult okResult = ((OkObjectResult)replaceResult);

            JObject jsonObject2 = JObject.Parse(okResult.Value.ToString());
            
            var insertedId = jsonObject2.SelectTokens(@"_id.$oid").FirstOrDefault().Value<string>();
            var insertedName = jsonObject2["name"].ToString();

            // Assert
            Assert.IsType(expectedReplace, okResult);
            Assert.Equal(id, insertedId);
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }")]
        [InlineData("7", "{ 'name': 'D' }")]
        // The service should replace Json objects with specified ID, and if asked to by the client, only return the IDs to the client instead of the entire Json object
        public async Task Replace_with_Primitive_Id_Sparse_Response(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act
            var insertResult = await controller.InsertObjectWithId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers10",
                Id = id
            }, json, ResponseFormat.OnlyId);

            // Act - do the replacement
            var replaceResult = await controller.ReplaceObject(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers10",
                Id = id
            }, json, ResponseFormat.OnlyId);

            OkObjectResult okResult = ((OkObjectResult)replaceResult);
            JObject jsonObject = JObject.Parse(okResult.Value.ToString());

            JArray jsonArray = JArray.Parse(jsonObject["ids"].ToString());
            var actualId = jsonArray[0].ToString();

            // Assert
            Assert.Equal(id, actualId);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Theory]
        [InlineData("{ \"name\": \"A\" }")]
        [InlineData("{ \"name\": 'C' }")]
        public async Task Replace_with_Replace_with_ObjectId_Response_Sparse_Response(string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act - insert first
            var insertResult = await controller.InsertObjectWithNoId(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers10",
            }, json, ResponseFormat.OnlyId);

            CreatedAtActionResult createdResult = ((CreatedAtActionResult)insertResult);
            JObject jsonObject = JObject.Parse(createdResult.Value.ToString());
            JArray jsonArray = JArray.Parse(jsonObject["ids"].ToString());
            var insertedId = jsonArray[0].ToString();

            // Act - do the replacement
            var replaceResult = await controller.ReplaceObject(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers10",
                Id = insertedId
            }, json, ResponseFormat.OnlyId);
            
            OkObjectResult okResult = ((OkObjectResult)replaceResult);

            JObject jsonObject2 = JObject.Parse(okResult.Value.ToString());
            JArray jsonArray2 = JArray.Parse(jsonObject2["ids"].ToString());
            var actualId = jsonArray2[0].ToString();

            // Assert
            Assert.Equal(insertedId, actualId);
            Assert.Equal(200, okResult.StatusCode);
        }


        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }")]
        [InlineData("6", "{ \"name\": 'C' }")]
        // The service should reject inserting an object on a replace operation if the object doesn't already exist
        public async Task Upsert_fails_Not_Found(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);

            // Act - do the replacement
            var replaceResult = await controller.ReplaceObject(new ItemRouteParameters() {
                DatabaseName = DATABASE_NAME,
                CollectionName = "customers3",
                Id = id
            }, json, ResponseFormat.EntireObject);

            var expectedReplace = typeof(ObjectResult);
            ObjectResult notFoundResult = ((ObjectResult)replaceResult);

            // Assert
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" ")]
        [InlineData("2", " \"name\": \"A\" }")]
        [InlineData("3", " \"name\": \"A\" ")]
        [InlineData("4", " ")]
        // Disallow updating an object with malformed Json
        public async Task Replace_fails_Malformed_Json(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            string collectionName = "customers4";

            try 
            {
                var result = await controller.ReplaceObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, json, ResponseFormat.OnlyId);
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                Assert.IsType<System.FormatException>(ex);
            }
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }")]
        // Disallow replacing an object in an immutable collection
        public async Task Replace_fails_Immutable_Collection(string id, string json)
        {
            var controller = new ObjectController(_fixture.MongoRepository);

            try 
            {
                var result = await controller.ReplaceObject(new ItemRouteParameters() { DatabaseName = "immutabledatabase", CollectionName = "immutablecollection", Id = id }, json, ResponseFormat.OnlyId);
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                Assert.IsType<Foundation.ObjectService.Exceptions.ImmutableCollectionException>(ex);
            }
        }

        #endregion // Single object replacements
        
        #region Single object deletion

        [Theory]
        [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }")]
        [InlineData("2", "{ \"title\": \"Don Quixote\" }")]
        public async Task Delete_Object_by_Primitive_Id(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "audit1";

            // Act
            var insertResult = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, json, ResponseFormat.OnlyId);

            var firstGetResult = await controller.GetObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id });

            OkObjectResult okGetResult = ((OkObjectResult)firstGetResult);
            Assert.Equal(200, okGetResult.StatusCode);

            var deleteResult = await controller.DeleteObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id });

            OkResult okDeleteResult = ((OkResult)deleteResult);
            Assert.Equal(200, okDeleteResult.StatusCode);

            var secondGetResult = await controller.GetObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id });

            ObjectResult notFoundGetResult = ((ObjectResult)secondGetResult);
            Assert.Equal(404, notFoundGetResult.StatusCode);
        }

        [Theory]
        [InlineData("{ \"title\": \"The Red Badge of Courage\" }")]
        [InlineData("{ \"title\": \"Don Quixote\" }")]
        public async Task Delete_Object_by_ObjectId(string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "audit2";

            // Act
            var insertResult = await controller.InsertObjectWithNoId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, json, ResponseFormat.OnlyId);
            CreatedAtActionResult createdResult = ((CreatedAtActionResult)insertResult);
            JObject jsonObject2 = JObject.Parse(createdResult.Value.ToString());
            var insertedId = JArray.Parse(jsonObject2["ids"].ToString())[0].ToString();

            var firstGetResult = await controller.GetObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = insertedId });

            OkObjectResult okGetResult = ((OkObjectResult)firstGetResult);
            Assert.Equal(200, okGetResult.StatusCode);

            var deleteResult = await controller.DeleteObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = insertedId });

            OkResult okDeleteResult = ((OkResult)deleteResult);
            Assert.Equal(200, okDeleteResult.StatusCode);

            var secondGetResult = await controller.GetObject(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = insertedId });

            ObjectResult notFoundGetResult = ((ObjectResult)secondGetResult);
            Assert.Equal(404, notFoundGetResult.StatusCode);
        }        

        #endregion // Single object deletion
    }

    public class ObjectControllerFixture : IDisposable
    {
        internal static MongoDbRunner _runner;

        public ILogger<MongoRepository> Logger { get; private set; }
        public IMongoClient MongoClient { get; private set; }
        public IObjectRepository MongoRepository { get; private set; }

        public ObjectControllerFixture()
        {
            Logger = new Mock<ILogger<MongoRepository>>().Object;
            _runner = MongoDbRunner.Start();
            MongoClient = new MongoClient(_runner.ConnectionString);

            var immutables = new Dictionary<string, HashSet<string>>();
            immutables.Add("immutabledatabase", new HashSet<string>() { "immutablecollection" });

            MongoRepository = new MongoRepository(MongoClient, Logger, immutables);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }
    }
}