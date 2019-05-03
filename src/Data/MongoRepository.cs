using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Foundation.ObjectService.Exceptions;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Driver.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Foundation.ObjectService.Data
{
    /// <summary>
    /// Class representing a MongoDB repository for arbitrary, untyped Json objects
    /// </summary>
    public class MongoRepository : IObjectRepository
    {
        private readonly IMongoClient _client = null;
        private readonly JsonWriterSettings _jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
        private readonly ILogger<MongoRepository> _logger;
        private const string ID_PROPERTY_NAME = "_id";
        private readonly Dictionary<string, HashSet<string>> _immutableCollections;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">MongoDB client</param>
        /// <param name="logger">Logger</param>
        /// <param name="immutableCollections">List of immutable collections</param>
        public MongoRepository(IMongoClient client, ILogger<MongoRepository> logger, Dictionary<string, HashSet<string>> immutableCollections)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (immutableCollections == null)
            {
                throw new ArgumentNullException(nameof(immutableCollections));
            }
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
            _logger = logger;
            _immutableCollections = immutableCollections;
        }

        /// <summary>
        /// Gets a single object
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object to get</param>
        /// <returns>The object matching the specified id</returns>
        public async Task<string> GetAsync(string databaseName, string collectionName, object id)
        {
            try
            {
                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                
                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                return StringifyDocument(await collection.Find(findDocument).FirstOrDefaultAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get failed on {databaseName}/{collectionName}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Gets all objects in a collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <returns>All objects in the collection</returns>
        public async Task<string> GetAllAsync(string databaseName, string collectionName)
        {
            try
            {
                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var documents = await collection.Find(_ => true).ToListAsync();
                return StringifyDocuments(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get all failed on {databaseName}/{collectionName}");
                throw;
            }
        }

        /// <summary>
        /// Inserts a single object into the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was inserted</returns>
        public async Task<string> InsertAsync(string databaseName, string collectionName, object id, string json)
        {
            try
            {
                if (_immutableCollections.ContainsKey(databaseName) && _immutableCollections[databaseName].Contains(collectionName))
                {
                    throw new ImmutableCollectionException($"Collection '{collectionName}' in database '{databaseName}' is immutable. No new items may be inserted.");
                }
                var _DoesDatabaseExist =  DoesDatabaseExist(databaseName);
                


                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var jsonObj = JObject.Parse(json);
                string OKey = (string)jsonObj.SelectToken("OKey");

                var document = BsonDocument.Parse(json);
                
                if (id != null)
                {
                    (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                    
                    if (isObjectId)
                    {
                        document.Set("_id", objectId);
                    }
                    else
                    {
                        document.Set("_id", id.ToString());
                    }
                }
                
                await collection.InsertOneAsync(document);
                id = document.GetValue("_id");

                if (!_DoesDatabaseExist && !string.IsNullOrEmpty(OKey) )
                {
                    var Username = "dbAdmin" + OKey;
                    var Password = OKey;
                    var User = AddUserAsync(databaseName, collectionName, Username, Password);
                }


                return await GetAsync(databaseName, collectionName, id);
            }
            catch (ImmutableCollectionException ex)
            {
                _logger.LogError(ex, $"Insert failed on {databaseName}/{collectionName}/{id}: Collection '{collectionName}' has been marked as immutable");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Insert failed on {databaseName}/{collectionName}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Updates a single object in the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was updated</returns>
        public async Task<string> ReplaceAsync(string databaseName, string collectionName, object id, string json)
        {
            try
            {
                if (_immutableCollections.ContainsKey(databaseName) && _immutableCollections[databaseName].Contains(collectionName))
                {
                    throw new ImmutableCollectionException($"Collection {collectionName} in database {databaseName} is immutable. No items may be replaced.");
                }

                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var document = BsonDocument.Parse(json);

                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var replaceOneResult = await collection.ReplaceOneAsync(findDocument, document);

                if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 1)
                {
                    return await GetAsync(databaseName, collectionName, id);
                }
                else if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 0)
                {
                    _logger.LogWarning($"Replace object attempted on {databaseName}/{collectionName}/{id}, but Mongo acknowledges with an updated count of 0");
                    return string.Empty;
                }
                else
                {
                    throw new InvalidOperationException("The replace operation was not acknowledged by MongoDB");
                }
            }
            catch (ImmutableCollectionException ex)
            {
                _logger.LogError(ex, $"Replace failed on {databaseName}/{collectionName}/{id}: Collection '{collectionName}' has been marked as immutable");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Replace failed on {databaseName}/{collectionName}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a single object in the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <returns>Whether the deletion was successful</returns>
        public async Task<bool> DeleteAsync(string databaseName, string collectionName, object id)
        {
            try
            {
                if (_immutableCollections.ContainsKey(databaseName) && _immutableCollections[databaseName].Contains(collectionName))
                {
                    throw new ImmutableCollectionException($"Collection {collectionName} in database {databaseName} is immutable. No items may be deleted.");
                }

                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);

                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var deleteOneResult = await collection.DeleteOneAsync(findDocument);

                if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 1)
                {
                    return true;
                }
                else if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 0)
                {
                    _logger.LogWarning($"Delete object attempted on {databaseName}/{collectionName}/{id}, but Mongo acknowledges with a deleted count of 0");
                    return false;
                }
                else
                {
                    throw new InvalidOperationException("The delete operation was not acknowledged by MongoDB");
                }
            }
            catch (ImmutableCollectionException ex)
            {
                _logger.LogError(ex, $"Delete failed on {databaseName}/{collectionName}/{id}: Collection '{collectionName}' has been marked as immutable");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Delete failed on {databaseName}/{collectionName}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <returns>Whether the deletion was successful</returns>
        public async Task<bool> DeleteCollectionAsync(string databaseName, string collectionName)
        {
            try
            {
                if (_immutableCollections.ContainsKey(databaseName) && _immutableCollections[databaseName].Contains(collectionName))
                {
                    throw new ImmutableCollectionException($"Collection {collectionName} in database {databaseName} is immutable. No items may be deleted.");
                }

                var database = GetDatabase(databaseName);
                bool collectionExists = await DoesCollectionExist(database, collectionName);

                if (collectionExists) 
                {
                    await database.DropCollectionAsync(collectionName);
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Delete collection attempted on {databaseName}/{collectionName}, but the collection does not exist");
                    return false;
                }
            }
            catch (ImmutableCollectionException ex)
            {
                _logger.LogError(ex, $"Delete collection failed on {databaseName}/{collectionName}: Collection '{collectionName}' has been marked as immutable");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Delete collection failed on {databaseName}/{collectionName}");
                throw;
            }
        }

        /// <summary>
        /// Finds a set of objects that match the specified find criteria
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <param name="start">The index within the find results at which to start filtering</param>
        /// <param name="size">The number of items within the find results to limit the result set to</param>
        /// <param name="sortFieldName">The Json property name of the object on which to sort</param>
        /// <param name="sortDirection">The sort direction</param>
        /// <returns>A collection of objects that match the find criteria</returns>
        public async Task<string> FindAsync(string databaseName, string collectionName, string findExpression, int start, int size, string sortFieldName, ListSortDirection sortDirection)
        {
            try
            {
                var regexFind = GetRegularExpressionQuery(databaseName, collectionName, findExpression, start, size, sortFieldName, sortDirection);
                var document = await regexFind.ToListAsync();
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                var stringifiedDocument = document.ToJson(jsonWriterSettings);
                return stringifiedDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Find failed on {databaseName}/{collectionName} with arguments start={start}, size={size}, sortFieldName={sortFieldName}");
                throw;
            }
        }

        /// <summary>
        /// Counts the number of objects that match the specified count criteria
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <returns>Number of matching objects</returns>
        public async Task<long> CountAsync(string databaseName, string collectionName, string findExpression)
        {
            try
            {
                var regexFind = GetRegularExpressionQuery(databaseName, collectionName, findExpression, 0, Int32.MaxValue, string.Empty, ListSortDirection.Ascending);
                var documentCount = await regexFind.CountDocumentsAsync();
                return documentCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Count failed on {databaseName}/{collectionName}");
                throw;
            }
        }

        /// <summary>
        /// Gets a list of distinct values for a given field
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="fieldName">The field name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <returns>List of distinct values</returns>
        public async Task<string> GetDistinctAsync(string databaseName, string collectionName, string fieldName, string findExpression)
        {
            try
            {
                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);

                BsonDocument bsonDocument = BsonDocument.Parse(findExpression);
                FilterDefinition<BsonDocument> filterDefinition = bsonDocument;

                var distinctResults = await collection.DistinctAsync<string>(fieldName, filterDefinition, null);
                
                var items = distinctResults.ToList();
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                var stringifiedDocument = items.ToJson(jsonWriterSettings);

                return stringifiedDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Distinct failed on {databaseName}/{collectionName}/distinct/{fieldName}");
                throw;
            }
        }

        /// <summary>
        /// Aggregates data via an aggregation pipeline and returns an array of objects
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="aggregationExpression">The MongoDB-style aggregation expression; see https://docs.mongodb.com/manual/aggregation/</param>
        /// <returns>List of matching objects</returns>
        public async Task<string> AggregateAsync(string databaseName, string collectionName, string aggregationExpression)
        {
            var database = GetDatabase(databaseName);
            var collection = GetCollection(database, collectionName);
            var pipeline = new List<BsonDocument>();

            var pipelineOperations = ParseJsonArray(aggregationExpression);
            foreach(var operation in pipelineOperations)
            {
                BsonDocument document = BsonDocument.Parse(operation);
                pipeline.Add(document);
            }

            var result = (await collection.AggregateAsync<BsonDocument> (pipeline)).ToList();

            var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            var stringifiedDocument = result.ToJson(jsonWriterSettings);

            return stringifiedDocument;
        }

        /// <summary>
        /// Inserts multiple objects and auto-generates their ids
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="jsonArray">The Json array that contains the objects to be inserted</param>
        /// <returns>List of ids that were generated for the inserted objects</returns>
        public async Task<string[]> InsertManyAsync(string databaseName, string collectionName, string jsonArray)
        {
            var database = GetDatabase(databaseName);
            var collection = GetCollection(database, collectionName);

            var documents = new List<BsonDocument>();

            JArray array = JArray.Parse(jsonArray);
            foreach(JObject o in array.Children<JObject>())
            {
                var json = o.ToString();
                BsonDocument document = BsonDocument.Parse(json);
                documents.Add(document);
            }
            
            await collection.InsertManyAsync(documents);

            List<string> ids = new List<string>();
            foreach (var document in documents)
            {
                ids.Add(document.GetValue("_id").ToString());
            }

            return ids.ToArray();
        }

        /// <summary>
        /// Parses a Json array into plain strings
        /// </summary>
        /// <remarks>
        /// This method is necessary because the JArray and other Json.Net APIs will throw exceptions when
        /// presented with invalid Json, e.g. MongoDB's find and aggregate syntax. While unfortunate, this
        /// method does work around the problem.
        /// </remarks>
        /// <param name="jsonArray">Json array to parse</param>
        /// <returns>List of string</returns>
        private List<string> ParseJsonArray(string jsonArray)
        {
            string array = jsonArray.Trim();
            var objects = new List<string>();

            if (!array.StartsWith("[") || !array.EndsWith("]"))
            {
                throw new ArgumentException("Json array must start and end with brackets", nameof(jsonArray));
            }

            var preparedArray = array.Substring(array.IndexOf('{')).TrimEnd(']').Trim(' ');

            int level = 0;
            int lastIndex = 0;

            for (int i = 0; i < preparedArray.Length; i++)
            {
                char character = preparedArray[i];

                if (character.Equals('{'))
                {
                    level++;
                }
                else if (character.Equals('}'))
                {
                    level--;

                    if (level == 0)
                    {
                        var obj = preparedArray.Substring(lastIndex, i - lastIndex + 1).Trim(',').Trim();
                        objects.Add(obj);
                        lastIndex = i + 1;
                    }
                }
            }

            return objects;
        }

        private IFindFluent<BsonDocument, BsonDocument> GetRegularExpressionQuery(string databaseName, string collectionName, string findExpression, int start, int size, string sortFieldName, ListSortDirection sortDirection)
        {
            var database = GetDatabase(databaseName);
            var collection = GetCollection(database, collectionName);

            if (size <= -1)
            {
                size = Int32.MaxValue;
            }

            BsonDocument bsonDocument = BsonDocument.Parse(findExpression);
            var regexFind = collection
                .Find(bsonDocument)
                .Skip(start)
                .Limit(size);

            if (!string.IsNullOrEmpty(sortFieldName))
            {
                if (sortDirection == ListSortDirection.Ascending)
                {
                    regexFind.SortBy(bson => bson[sortFieldName]);
                }
                else
                {
                    regexFind.SortByDescending(bson => bson[sortFieldName]);
                }
            }

            return regexFind;
        }

        /// <summary>
        /// Returns whether or not the collection exists in the specified 
        /// </summary>
        /// <param name="databaseName">Name of the database that owns the specified collection</param>
        /// <param name="collectionName">Name of the collection to check</param>
        /// <returns>bool; whether or not the collection eixsts</returns>
        public async Task<bool> DoesCollectionExist(string databaseName, string collectionName)
        {
            var database = GetDatabase(databaseName);
            return await DoesCollectionExist(database, collectionName);
        }

        private async Task<bool> DoesCollectionExist(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collectionCursor = await database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
            var exists = await collectionCursor.AnyAsync();
            return exists;
        }
        private bool DoesDatabaseExist(string database )
        {
             var List =   _client.ListDatabaseNames().ToList();
              return    List.Contains(database);
                      }
        /// <summary>
        /// Forces an ID property into a JSON object
        /// </summary>
        /// <param name="id">The ID value to force into the object's 'id' property</param>
        /// <param name="json">The Json that should contain the ID key and value</param>
        /// <returns>The Json object with an 'id' property and the specified id value</returns>
        private string ForceAddIdToJsonObject(object id, string json)
        {
            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (values.ContainsKey(ID_PROPERTY_NAME))
            {
                values[ID_PROPERTY_NAME] = id;
            }
            else
            {
                values.Add(ID_PROPERTY_NAME, id);
            }
            string checkedJson = Newtonsoft.Json.JsonConvert.SerializeObject(values, Formatting.Indented);
            return checkedJson;
        }

        private IMongoDatabase GetDatabase(string databaseName)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            #endregion // Input Validation
            return _client.GetDatabase(databaseName);
        }

        private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase database, string collectionName)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentNullException(nameof(collectionName));
            }
            #endregion // Input Validation
            return database.GetCollection<BsonDocument>(collectionName);
        }

        private string StringifyDocument(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }
            return document.ToJson(_jsonWriterSettings);
        }

        private string StringifyDocuments(List<BsonDocument> documents) => documents.ToJson(_jsonWriterSettings);

        private (bool, ObjectId) IsObjectId(string id)
        {
            bool isObjectId = ObjectId.TryParse(id.ToString(), out ObjectId objectId);
            return (isObjectId, objectId);
        }
        /// <summary>
        /// Returns whether or not the user is added
        /// </summary>
        /// <param name="databaseName">Name of the database that owns the specified collection</param>
        /// <param name="collectionName">Name of the collection to check</param>
        /// /// <param name="Username">Username</param>
        /// <param name="Password">Password</param>
        /// <returns>bool; whether or not the user got added</returns>
        private async Task<bool> AddUserAsync(string databaseName, string collectionName, string Username, string Password)
        {
            try
            {
                 
                var db = GetDatabase(databaseName);
                var user = new BsonDocument
                {
                    { "createUser", Username },
                    { "pwd", Password },
                    { "roles", new BsonArray
                        { new BsonDocument
                            {
                                { "role", "dbOwner" },
                                { "db", "" + databaseName + "" },
                                { "collection", "" + collectionName + "" }
                            }
                        }
                    }
                };
                await db.RunCommandAsync<BsonDocument>(user);
                return true;
            }
            catch (Exception ex)
            {
                return false;
                _logger.LogError(ex, $"Add user failed !");
                throw;
            }
        }
    }
}