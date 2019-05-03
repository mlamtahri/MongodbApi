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
        #region Find and search

        [Theory]
        [InlineData("books101", "pages>464", 2)]
        [InlineData("books101", "pages>=464", 3)]
        [InlineData("books102", "pages<464", 7)]
        [InlineData("books103", "pages>=288", 5)]
        [InlineData("books103", "pages:288", 2)]
        [InlineData("books104", "pages!:288", 8)]
        [InlineData("books105", "title:Slaughterhouse-Five", 1)]
        [InlineData("books105", "title:\"Slaughterhouse-Five\"", 1)]
        [InlineData("books106", "title:\"The Red Badge of Courage\" pages>50", 1)]
        [InlineData("books107", "title:\"The Great Gatsby\" pages>250", 0)]
        [InlineData("books108", "title:\"The Great Gatsby\" pages<250", 1)]
        [InlineData("books109", "title:\"The Great Gatsby\" pages<250 author:\"F. Scott Fitzgerald\"", 1)]
        [InlineData("books110", "author:\"John Steinbeck\"", 2)]
        [InlineData("books111", "author:\"John Steinbeck\" pages<=464", 2)]
        [InlineData("books112", "author:\"John Steinbeck\" pages<464", 1)]
        [InlineData("books113", "pages<464 author:\"John Steinbeck\"", 1)]
        [InlineData("books114", "author:\"Cervantes\"", 0)]
        [InlineData("books115", "author!:\"John Steinbeck\"", 8)]
        public async Task Search_Collection(string collectionName, string qs, int expectedCount)
        {
            var controller = new ObjectController(_fixture.MongoRepository);

            var items = new List<string>() 
            {
                "{ 'title': 'The Red Badge of Courage', 'author': 'Stephen Crane', 'pages': 112, 'isbn': { 'isbn-10' : '0486264653', 'isbn-13' : '978-0486264653' } }",
                "{ 'title': 'Don Quixote', 'author': 'Miguel De Cervantes', 'pages': 992, 'isbn': { 'isbn-10' : '0060934344', 'isbn-13' : '978-0060934347' } }",
                "{ 'title': 'The Grapes of Wrath', 'author': 'John Steinbeck', 'pages': 464, 'isbn': { 'isbn-10' : '0143039431', 'isbn-13' : '978-0143039433' } }",
                "{ 'title': 'The Catcher in the Rye', 'author': 'J. D. Salinger', 'pages': 288, 'isbn': { 'isbn-10' : '9780316769174', 'isbn-13' : '978-0316769174' } }",
                "{ 'title': 'Slaughterhouse-Five', 'author': 'Kurt Vonnegut', 'pages': 288, 'isbn': { 'isbn-10' : '0812988523', 'isbn-13' : '978-0812988529' } }",
                "{ 'title': 'Of Mice and Men', 'author': 'John Steinbeck', 'pages': 112, 'isbn': { 'isbn-10' : '0140177396', 'isbn-13' : '978-0140177398' } }",
                "{ 'title': 'Gone with the Wind', 'author': 'Margaret Mitchell', 'pages': 960, 'isbn': { 'isbn-10' : '1451635621', 'isbn-13' : '978-1451635621' } }",
                "{ 'title': 'Fahrenheit 451', 'author': 'Ray Bradbury', 'pages': 249, 'isbn': { 'isbn-10' : '9781451673319', 'isbn-13' : '978-1451673319' } }",
                "{ 'title': 'The Old Man and the Sea', 'author': 'Ernest Hemingway', 'pages': 128, 'isbn': { 'isbn-10' : '0684801221', 'isbn-13' : '978-0684801223' } }",
                "{ 'title': 'The Great Gatsby', 'author': 'F. Scott Fitzgerald', 'pages': 180, 'isbn': { 'isbn-10' : '9780743273565', 'isbn-13' : '978-0743273565' } }",
            };

            var payload = "[" + string.Join(',', items) + "]";
            var insertManyResult = await controller.MultiInsert(new DatabaseRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, payload);
            var insertManyMvcResult = ((OkObjectResult)insertManyResult);
            Assert.Equal(200, insertManyMvcResult.StatusCode);

            var searchResult = await controller.SearchObjects(qs, new DatabaseRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, new FindQueryParameters());
            var searchMvcResult = ((OkObjectResult)searchResult);
            Assert.Equal(200, searchMvcResult.StatusCode);

            var array = JArray.Parse(searchMvcResult.Value.ToString());
            Assert.Equal(expectedCount, array.Count);

            // Delete the collection
            var deleteCollectionResult = await controller.DeleteCollection(new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName });
            var deleteCollectionMvcResult = ((OkResult)deleteCollectionResult);
            Assert.Equal(200, deleteCollectionMvcResult.StatusCode);
        }

        [Theory]
        [InlineData("books201", "{ pages: 288 }", 0, -1, 2)]
        [InlineData("books202", "{ pages: 288 }", 0, 1, 1)]
        [InlineData("books203", "{ pages: 288 }", 1, 1, 1)]
        [InlineData("books204", "{ pages: 289 }", 0, -1, 0)]
        [InlineData("books205", "{ pages: { $lt: 150 } }", 0, -1, 3)]
        [InlineData("books206", "{ pages: { $lt: 112 } }", 0, -1, 0)]
        [InlineData("books207", "{ pages: { $lte: 112 } }", 0, -1, 2)]
        [InlineData("books208", "{ pages: { $gt: 150 } }", 0, -1, 7)]
        [InlineData("books209", "{ pages: { $gt: 464 } }", 0, -1, 2)]
        [InlineData("books210", "{ pages: { $gte: 464 } }", 0, -1, 3)]
        [InlineData("books211", "{ title: /^(the|a)/i }", 0, -1, 5)]
        [InlineData("books212", "{ title: /^(the|of)/i }", 0, -1, 6)]
        [InlineData("books213", "{ title: /^(g)/i }", 0, -1, 1)]
        [InlineData("books214", "{ title: /^(the|of)/i, pages: { $gt: 300 } }", 0, -1, 1)]
        [InlineData("books215", "{ title: /^(the|of)/i, pages: { $lt: 500 }, author:'John Steinbeck' }", 0, -1, 2)]
        [InlineData("books216", "{ title: /^(the|of)/i, pages: { $lt: 500 }, author:\"John Steinbeck\" }", 0, -1, 2)]
        [InlineData("books217", "{ title: /^(the|of)/i, pages: { $lt: 500 }, author: /^(john)/i }", 0, -1, 2)]
        public async Task Find_Objects_in_Collection(string collectionName, string findExpression, int start, int limit, int expectedCount)
        {
            var controller = new ObjectController(_fixture.MongoRepository);

            var items = new List<string>() 
            {
                "{ 'title': 'The Red Badge of Courage', 'author': 'Stephen Crane', 'pages': 112, 'isbn': { 'isbn-10' : '0486264653', 'isbn-13' : '978-0486264653' } }",
                "{ 'title': 'Don Quixote', 'author': 'Miguel De Cervantes', 'pages': 992, 'isbn': { 'isbn-10' : '0060934344', 'isbn-13' : '978-0060934347' } }",
                "{ 'title': 'The Grapes of Wrath', 'author': 'John Steinbeck', 'pages': 464, 'isbn': { 'isbn-10' : '0143039431', 'isbn-13' : '978-0143039433' } }",
                "{ 'title': 'The Catcher in the Rye', 'author': 'J. D. Salinger', 'pages': 288, 'isbn': { 'isbn-10' : '9780316769174', 'isbn-13' : '978-0316769174' } }",
                "{ 'title': 'Slaughterhouse-Five', 'author': 'Kurt Vonnegut', 'pages': 288, 'isbn': { 'isbn-10' : '0812988523', 'isbn-13' : '978-0812988529' } }",
                "{ 'title': 'Of Mice and Men', 'author': 'John Steinbeck', 'pages': 112, 'isbn': { 'isbn-10' : '0140177396', 'isbn-13' : '978-0140177398' } }",
                "{ 'title': 'Gone with the Wind', 'author': 'Margaret Mitchell', 'pages': 960, 'isbn': { 'isbn-10' : '1451635621', 'isbn-13' : '978-1451635621' } }",
                "{ 'title': 'Fahrenheit 451', 'author': 'Ray Bradbury', 'pages': 249, 'isbn': { 'isbn-10' : '9781451673319', 'isbn-13' : '978-1451673319' } }",
                "{ 'title': 'The Old Man and the Sea', 'author': 'Ernest Hemingway', 'pages': 128, 'isbn': { 'isbn-10' : '0684801221', 'isbn-13' : '978-0684801223' } }",
                "{ 'title': 'The Great Gatsby', 'author': 'F. Scott Fitzgerald', 'pages': 180, 'isbn': { 'isbn-10' : '9780743273565', 'isbn-13' : '978-0743273565' } }",
            };

            var payload = "[" + string.Join(',', items) + "]";
            var insertManyResult = await controller.MultiInsert(new DatabaseRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, payload);
            var insertManyMvcResult = ((OkObjectResult)insertManyResult);
            Assert.Equal(200, insertManyMvcResult.StatusCode);

            var findResult = await controller.FindObjects(new DatabaseRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, new FindQueryParameters() { Start = start, Limit = limit }, findExpression);
            var findMvcResult = ((OkObjectResult)findResult);
            Assert.Equal(200, findMvcResult.StatusCode);

            var array = JArray.Parse(findMvcResult.Value.ToString());
            Assert.Equal(expectedCount, array.Count);

            // Delete the collection
            var deleteCollectionResult = await controller.DeleteCollection(new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName });
            var deleteCollectionMvcResult = ((OkResult)deleteCollectionResult);
            Assert.Equal(200, deleteCollectionMvcResult.StatusCode);
        }

        #endregion // Find and search
    }
}