using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Foundation.ObjectService.Data
{
    /// <summary>
    /// Interface representing a repository for arbitrary, untyped Json objects
    /// </summary>
    public interface IObjectRepository
    {
        /// <summary>
        /// Gets a single object
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object to get</param>
        /// <returns>The object matching the specified id</returns>
        Task<string> GetAsync(string databaseName, string collectionName, object id);

        /// <summary>
        /// Gets all objects in a collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <returns>All objects in the collection</returns>
        Task<string> GetAllAsync(string databaseName, string collectionName);

        /// <summary>
        /// Inserts a single object into the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was inserted</returns>
        Task<string> InsertAsync(string databaseName, string collectionName, object id, string json);

        /// <summary>
        /// Updates a single object in the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was updated</returns>
        Task<string> ReplaceAsync(string databaseName, string collectionName, object id, string json);

        /// <summary>
        /// Deletes a single object in the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <returns>Whether the deletion was successful</returns>
        Task<bool> DeleteAsync(string databaseName, string collectionName, object id);

        /// <summary>
        /// Deletes a collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <returns>Whether the deletion was successful</returns>
        Task<bool> DeleteCollectionAsync(string databaseName, string collectionName);

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
        Task<string> FindAsync(string databaseName, string collectionName, string findExpression, int start, int size, string sortFieldName, ListSortDirection sortDirection);

        /// <summary>
        /// Counts the number of objects that match the specified count criteria
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <returns>Number of matching objects</returns>
        Task<long> CountAsync(string databaseName, string collectionName, string findExpression);

        /// <summary>
        /// Gets a list of distinct values for a given field
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="fieldName">The field name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <returns>List of distinct values</returns>
        Task<string> GetDistinctAsync(string databaseName, string collectionName, string fieldName, string findExpression);

        /// <summary>
        /// Aggregates data via an aggregation pipeline and returns an array of objects
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="aggregationExpression">The MongoDB-style aggregation expression; see https://docs.mongodb.com/manual/aggregation/</param>
        /// <returns>List of matching objects</returns>
        Task<string> AggregateAsync(string databaseName, string collectionName, string aggregationExpression);

        /// <summary>
        /// Inserts multiple objects and auto-generates their ids
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="jsonArray">The Json array that contains the objects to be inserted</param>
        /// <returns>List of ids that were generated for the inserted objects</returns>
        Task<string[]> InsertManyAsync(string databaseName, string collectionName, string jsonArray);

        /// <summary>
        /// Returns whether or not the collection exists in the specified 
        /// </summary>
        /// <param name="databaseName">Name of the database that owns the specified collection</param>
        /// <param name="collectionName">Name of the collection to check</param>
        /// <returns>bool; whether or not the collection eixsts</returns>
        Task<bool> DoesCollectionExist(string databaseName, string collectionName);
    
    }
}