using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Foundation.ObjectService.Data;
using Foundation.ObjectService.Exceptions;
using Foundation.ObjectService.ViewModel;

using Newtonsoft.Json.Linq;

using Swashbuckle.AspNetCore.Annotations;

using ChoETL;

namespace Foundation.ObjectService.WebUI.Controllers
{
    /// <summary>
    /// Object service controller class
    /// </summary>
    [Route("api/1.0")]
    [ApiController]
    [SwaggerResponse(400, "Client error, such as invalid inputs or malformed Json")]
    [SwaggerResponse(401, "HTTP header lacks a valid OAuth2 token")]
    [SwaggerResponse(403, "HTTP header has a valid OAuth2 token but lacks the appropriate scope to use this route")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ObjectController : ControllerBase
    {
        private readonly IObjectRepository _repository;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository">The object repository to use for interacting with the underlying database</param>
        public ObjectController(IObjectRepository repository)
        {
            _repository = repository;
        }

        // GET api/1.0/db/collection/5
        /// <summary>
        /// Gets an object
        /// </summary>
        /// <param name="routeParameters">Required route parameters needed for the find operation</param>
        /// <returns>Object that has the matching id</returns>
        [Produces("application/json")]
        [HttpGet("{db}/{collection}/{id}")]
        [SwaggerResponse(200, "Object returned successfully")]        
        [SwaggerResponse(404, "The specified object or collection could not be found")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> GetObject([FromRoute] ItemRouteParameters routeParameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var document = await _repository.GetAsync(routeParameters.DatabaseName, routeParameters.CollectionName, routeParameters.Id);
            if (document == null)
            {
                return ObjectNotFound(routeParameters.Id, routeParameters.CollectionName);
            }
            return Ok(document);
        }

        // POST api/1.0/db/collection/6
        /// <summary>
        /// Inserts an object with a specified ID
        /// </summary>
        /// <remarks>
        /// Sample request to insert a new Json document with an id of 6:
        ///
        ///     POST /api/1.0/db/collection/6
        ///     {
        ///         "status": "A",
        ///         "code": 200
        ///     }
        ///
        /// </remarks>
        /// <param name="routeParameters">Required route parameters needed for the operation</param>
        /// <param name="json">The Json representation of the object to insert</param>
        /// <param name="responseFormat">The format of the response type; defaults to 0</param>
        /// <returns>Object that was inserted</returns>
        [Produces("application/json")]
        [Consumes("application/json")]
        [HttpPost("{db}/{collection}/{id}")]
        [SwaggerResponse(201, "Returns the inserted object")]
        [SwaggerResponse(406, "Invalid content type")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.INSERT_AUTHORIZATION_NAME)]
        public async Task<IActionResult> InsertObjectWithId([FromRoute] ItemRouteParameters routeParameters, [FromBody] string json, [FromQuery] ResponseFormat responseFormat)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            string document = await _repository.InsertAsync(routeParameters.DatabaseName, routeParameters.CollectionName, routeParameters.Id, json);

            if (responseFormat == ResponseFormat.OnlyId)
            {
                document = document = GetInsertedJsonResult(new string [] { routeParameters.Id }).ToString();
            }
            return CreatedAtAction(nameof(GetObject), new { id = routeParameters.Id, db = routeParameters.DatabaseName, collection = routeParameters.CollectionName }, document);
        }

        // POST api/1.0/db/collection
        /// <summary>
        /// Inserts an object without a specified ID
        /// </summary>
        /// <remarks>
        /// Sample request to insert a new Json document:
        ///
        ///     POST /api/1.0/db/collection
        ///     {
        ///         "status": "A",
        ///         "code": 200
        ///     }
        ///
        /// </remarks>
        /// <param name="routeParameters">Required route parameters needed for the operation</param>
        /// <param name="json">The Json representation of the object to insert</param>
        /// <param name="responseFormat" default="0">The format of the response type; defaults to 0</param>
        /// <returns>Object that was inserted</returns>
        [Produces("application/json")]
        [Consumes("application/json")]
        [HttpPost("{db}/{collection}")]
        [SwaggerResponse(201, "Returns the inserted object")]
        [SwaggerResponse(406, "Invalid content type")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.INSERT_AUTHORIZATION_NAME)]
        public async Task<IActionResult> InsertObjectWithNoId([FromRoute] DatabaseRouteParameters routeParameters, [FromBody] string json, [FromQuery] ResponseFormat responseFormat = ResponseFormat.EntireObject)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string document = await _repository.InsertAsync(routeParameters.DatabaseName, routeParameters.CollectionName, null, json);            
            string id = GetObjectId(document);

            if (responseFormat == ResponseFormat.OnlyId)
            {
                document = GetInsertedJsonResult(new string [] { id }).ToString();
            }
            return CreatedAtAction(nameof(GetObject), new { id = id, db = routeParameters.DatabaseName, collection = routeParameters.CollectionName }, document);
        }

        // PUT api/1.0/db/collection/5
        /// <summary>
        /// Updates an object
        /// </summary>
        /// <remarks>
        /// Sample request to conduct a wholesale replacement of the object with an id of 6:
        ///
        ///     PUT /api/1.0/db/collection/6
        ///     {
        ///         "status": "D",
        ///         "code": 400
        ///     }
        ///
        /// </remarks>
        /// <param name="routeParameters">Required route parameters needed for the operation</param>
        /// <param name="json">The Json representation of the object to update</param>
        /// <param name="responseFormat">The format of the response type; defaults to 0</param>
        /// <returns>The updated object</returns>
        [Produces("application/json")]
        [Consumes("application/json")]
        [HttpPut("{db}/{collection}/{id}")]
        [SwaggerResponse(200, "Returns the updated object")]
        [SwaggerResponse(404, "The specified object or collection could not be found")]
        [SwaggerResponse(406, "Invalid content type")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.UPDATE_AUTHORIZATION_NAME)]
        public async Task<IActionResult> ReplaceObject([FromRoute] ItemRouteParameters routeParameters, [FromBody] string json, [FromQuery] ResponseFormat responseFormat = ResponseFormat.EntireObject)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            string document = await _repository.ReplaceAsync(routeParameters.DatabaseName, routeParameters.CollectionName, routeParameters.Id, json);

            if (string.IsNullOrEmpty(document))
            {
                return ObjectNotFound(routeParameters.Id, routeParameters.CollectionName);
            }

            if (responseFormat == ResponseFormat.OnlyId)
            {
                string id = GetObjectId(document);
                document = GetReplacedJsonResult(new string [] { id }).ToString();
                return Ok(document);
            }
            else 
            {
                return Ok(document);
            }
        }

        // DELETE api/1.0/db/collection/5
        /// <summary>
        /// Deletes an object
        /// </summary>
        /// <param name="routeParameters">Required route parameters needed for the operation</param>
        /// <returns>Whether the item was deleted or not</returns>
        [Produces("application/json")]
        [HttpDelete("{db}/{collection}/{id}")]
        [SwaggerResponse(200, "The object was deleted successfully", typeof(bool))]
        [SwaggerResponse(404, "The specified object or collection could not be found")]
        [Authorize(Common.DELETE_AUTHORIZATION_NAME)]
        public async Task<IActionResult> DeleteObject([FromRoute] ItemRouteParameters routeParameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool deleted = await _repository.DeleteAsync(routeParameters.DatabaseName, routeParameters.CollectionName, routeParameters.Id);

            if (deleted)
            {
                return Ok();
            }
            else
            {
                return ObjectNotFound(routeParameters.Id, routeParameters.CollectionName);
            }
        }

        // DELETE api/1.0/db/collection
        /// <summary>
        /// Deletes a collection
        /// </summary>
        /// <param name="routeParameters">Required route parameters needed for the operation</param>
        /// <returns>Whether the collection was deleted or not</returns>
        [Produces("application/json")]
        [HttpDelete("{db}/{collection}")]
        [SwaggerResponse(200, "The collection was deleted successfully", typeof(bool))]
        [SwaggerResponse(404, "The specified collection could not be found")]
        [Authorize(Common.DELETE_AUTHORIZATION_NAME)]
        public async Task<IActionResult> DeleteCollection([FromRoute] DatabaseRouteParameters routeParameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var exists = await _repository.DoesCollectionExist(routeParameters.DatabaseName, routeParameters.CollectionName);
            if (!exists)
            {
                return CollectionNotFound(routeParameters.CollectionName);
            }

            var deleted = await _repository.DeleteCollectionAsync(routeParameters.DatabaseName, routeParameters.CollectionName);
            if (deleted)
            {
                return Ok();
            }
            else
            {
                return CollectionNotFound(routeParameters.CollectionName);
            }
        }

        // POST api/1.0/db/collection/find
        /// <summary>
        /// Finds one or more objects that match the specified criteria
        /// </summary>
        /// <remarks>
        /// Sample request to find one or more documents with a status of either 'A' or 'D' and that have a code equal to 400:
        ///
        ///     POST /api/1.0/db/collection/find
        ///     {
        ///         status:
        ///         {
        ///             $in: [ "A", "D" ]
        ///         },
        ///         code: 400
        ///     }
        ///
        /// </remarks>
        /// <param name="findExpression">The Json find expression</param>
        /// <param name="routeParameters">Required route parameters needed for the find operation</param>
        /// <param name="queryParameters">Additional optional parameters to use for the find operation</param>
        /// <returns>Array of objects that match the provided regular expression and inputs</returns>
        [Produces("application/json")]
        [Consumes("text/plain")]
        [HttpPost("{db}/{collection}/find")]
        [SwaggerResponse(200, "Returns the objects that match the inputs to the find operation")]
        [SwaggerResponse(404, "The specified collection could not be found")]
        [SwaggerResponse(406, "The request body was submitted as something other than text/plain")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> FindObjects([FromRoute] DatabaseRouteParameters routeParameters, [FromQuery] FindQueryParameters queryParameters, [FromBody] string findExpression)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var findResults = await _repository.FindAsync(routeParameters.DatabaseName, routeParameters.CollectionName, findExpression, queryParameters.Start, queryParameters.Limit, queryParameters.SortFieldName, System.ComponentModel.ListSortDirection.Ascending);
            return Ok(findResults);
        }

        // GET api/1.0/db/collection/search
        /// <summary>
        /// Searches for one or more objects that match the specified criteria
        /// </summary>
        /// <remarks>
        /// Sample request to search for one or more documents with a status of 'A'
        ///
        ///     GET /api/1.0/db/collection/search?qs=status%3AA
        ///
        /// </remarks>
        /// <param name="qs">The plain text search expression</param>
        /// <param name="routeParameters">Required route parameters needed for the find operation</param>
        /// <param name="queryParameters">Additional optional parameters to use for the find operation</param>
        /// <returns>Array of objects that match the provided regular expression and inputs</returns>
        [Produces("application/json")]
        [HttpGet("{db}/{collection}/search")]
        [SwaggerResponse(200, "Returns the objects that match the inputs to the search operation")]
        [SwaggerResponse(404, "The specified collection could not be found")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> SearchObjects([FromQuery] string qs, [FromRoute] DatabaseRouteParameters routeParameters, [FromQuery] FindQueryParameters queryParameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var findExpression = SearchStringConverter.BuildQuery(qs);
            var findResults = await _repository.FindAsync(routeParameters.DatabaseName, routeParameters.CollectionName, findExpression, queryParameters.Start, queryParameters.Limit, queryParameters.SortFieldName, System.ComponentModel.ListSortDirection.Ascending);
            return Ok(findResults);
        }

        // GET api/1.0/db/collection
        /// <summary>
        /// Gets all objects in a collection
        /// </summary>
        /// <param name="routeParameters">Required route parameters needed for the find operation</param>
        /// <returns>Array of objects in the specified collection</returns>
        [Produces("application/json")]
        [HttpGet("{db}/{collection}")]
        [SwaggerResponse(200, "Returns all objects in the specified collection")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> GetAllObjectsInCollection([FromRoute] DatabaseRouteParameters routeParameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var exists = await _repository.DoesCollectionExist(routeParameters.DatabaseName, routeParameters.CollectionName);
            if (!exists)
            {
                return CollectionNotFound(routeParameters.CollectionName);
            }
            
            var findResults = await _repository.GetAllAsync(routeParameters.DatabaseName, routeParameters.CollectionName);
            return Ok(findResults);
        }

        // POST api/1.0/db/collection/count
        /// <summary>
        /// Counts how many objects match the specified criteria
        /// </summary>
        /// <remarks>
        /// Sample request to count the number of documents with a status of either 'A' or 'D' and that have a code equal to 400:
        ///
        ///     POST /api/1.0/object/count
        ///     {
        ///         status:
        ///         {
        ///             $in: [ "A", "D" ]
        ///         },
        ///         code: 400
        ///     }
        ///
        /// </remarks>
        /// <param name="countExpression">The Json count expression</param>
        /// <param name="routeParameters">Required route parameters needed for the count operation</param>
        /// <returns>Number of objects that match the provided regular expression and inputs</returns>
        [Produces("application/json")]
        [Consumes("text/plain")]
        [HttpPost("{db}/{collection}/count")]
        [SwaggerResponse(200, "Returns the number of objects that match the inputs to the count operation")]
        [SwaggerResponse(406, "The request body was submitted as something other than text/plain")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> CountObjects([FromRoute] DatabaseRouteParameters routeParameters, [FromBody] string countExpression)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var countResults = await _repository.CountAsync(routeParameters.DatabaseName, routeParameters.CollectionName, countExpression);
            return Ok(countResults);
        }

        // POST api/1.0/db/collection/distinct/status
        /// <summary>
        /// Gets an array of distinct values for a given field
        /// </summary>
        /// <remarks>
        /// Sample request to get the distinct values for the 'status' field:
        ///
        ///     POST /api/1.0/object/distinct/status
        ///     {}
        ///
        /// </remarks>
        /// <param name="findExpression">The Json find expression</param>
        /// <param name="routeParameters">Required route parameters needed for the distinct operation</param>
        /// <param name="field">The field whose distinct values should be returned</param>
        /// <returns>Array of distinct values for the specified field name, filtered by the specified find expression</returns>
        [Produces("application/json")]
        [Consumes("text/plain")]
        [HttpPost("{db}/{collection}/distinct/{field}")]
        [SwaggerResponse(200, "Returns the distinct values for the specified field name, filtered by the specified find expression")]
        [SwaggerResponse(404, "The specified collection could not be found")]
        [SwaggerResponse(406, "The request body was submitted as something other than text/plain")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> Distinct([FromRoute] DatabaseRouteParameters routeParameters, [FromRoute] string field, [FromBody] string findExpression)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string distinctResults = await _repository.GetDistinctAsync(routeParameters.DatabaseName, routeParameters.CollectionName, field, findExpression);
            return Ok(distinctResults);
        }

        // POST api/1.0/db/collection/aggregate
        /// <summary>
        /// Processes and returns data through an aggregation pipeline
        /// </summary>
        /// <param name="payload">The Json aggregation payload</param>
        /// <param name="routeParameters">Required route parameters needed for the aggregate operation</param>
        /// <returns>Array objects</returns>
        [Produces("application/json")]
        [Consumes("text/plain")]
        [HttpPost("{db}/{collection}/aggregate")]
        [SwaggerResponse(200, "Returns an array of objects")]
        [SwaggerResponse(404, "The specified collection could not be found")]
        [SwaggerResponse(406, "The request body was submitted as something other than text/plain")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> Aggregate([FromRoute] DatabaseRouteParameters routeParameters, [FromBody] string payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var aggregateResults = await _repository.AggregateAsync(routeParameters.DatabaseName, routeParameters.CollectionName, payload);
            return Ok(aggregateResults);
        }

        // POST api/1.0/multi/db/collection
        /// <summary>
        /// Inserts multiple objects at a time and auto-generates IDs for each object
        /// </summary>
        /// <remarks>
        /// Sample request to insert four books into the books collection in the bookstore database:
        ///
        ///     POST /api/1.0/multi/bookstore/books
        ///     [
        ///         { "title": "Don Quixote", "author" : " Miguel De Cervantes", "pages": 992 },
        ///         { "title": "The Secret Garden", "author" : "Frances Hodgson Burnett", "pages": 126 },
        ///         { "title": "Moby Dick; Or The Whale", "author" : "Herman Melville", "pages": 458 },
        ///         { "title": "Faust", "author" : "Johann Wolfgang Von Goethe", "pages": 158 }
        ///     ]
        ///
        /// </remarks>
        /// <param name="payload">The Json array of objects</param>
        /// <param name="routeParameters">Required route parameters needed for the multi-insert operation</param>
        /// <returns>Array of ids for the inserted objects and a count of how many objects were inserted</returns>
        [Produces("application/json")]
        [Consumes("application/json")]
        [HttpPost("multi/{db}/{collection}")]
        [SwaggerResponse(200, "Returns an array of ids for the inserted objects and a count of how many objects were inserted")]
        [SwaggerResponse(404, "The specified collection could not be found")]
        [SwaggerResponse(406, "The request body was submitted as something other than application/json")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> MultiInsert([FromRoute] DatabaseRouteParameters routeParameters, [FromBody] string payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var results = await _repository.InsertManyAsync(routeParameters.DatabaseName, routeParameters.CollectionName, payload);
            return Ok(GetInsertedJsonResult(results));
        }

        // POST api/1.0/bulk/db/collection
        /// <summary>
        /// Inserts multiple objects at a time from a CSV file and auto-generates IDs for each object
        /// </summary>
        /// <param name="csv">The Csv file of objects to insert</param>
        /// <param name="routeParameters">Required route parameters needed for the bulk insert operation</param>
        /// <returns>Array of ids for the inserted objects and a count of how many objects were inserted</returns>
        [Produces("application/json")]
        [Consumes("multipart/form-data")]
        [HttpPost("bulk/{db}/{collection}")]
        [SwaggerResponse(200, "Returns an array of ids for the inserted objects and a count of how many objects were inserted")]
        [SwaggerResponse(404, "The specified collection could not be found")]
        [SwaggerResponse(406, "The request body was submitted as something other than application/json")]
        [SwaggerResponse(413, "The request payload is too large")]
        [SwaggerResponse(415, "Invalid media type")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<IActionResult> MultiInsertFromCsv([FromRoute] DatabaseRouteParameters routeParameters, IFormFile csv)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            List<string> rows = new List<string>();
            
            if (csv.Length > 0)
            {
                var result = string.Empty;

                using (var reader = new System.IO.StreamReader(csv.OpenReadStream()))
                {
                    using (var csvReader = new ChoCSVReader(reader)
                        .WithFirstLineHeader())
                    {
                        foreach (var row in csvReader) 
                        {
                            rows.Add(row.DumpAsJson());
                        }
                    }
                }

                string payload = "[" + string.Join(',', rows) + "]";
                var results = await _repository.InsertManyAsync(routeParameters.DatabaseName, routeParameters.CollectionName, payload);

                return Ok(GetInsertedJsonResult(results));
            }
            else
            {
                return BadRequestDetail("Csv file has no data");
            }
        }

        private JObject GetInsertedJsonResult(string [] results) => new JObject(
            new JProperty("inserted", results.Length), 
            new JProperty("ids", 
                new JArray(results)));

        private JObject GetReplacedJsonResult(string [] results) => new JObject(
            new JProperty("updated", results.Length), 
            new JProperty("ids", 
                new JArray(results)));

        private string GetObjectId(string document)
        {
            string id = string.Empty;            
            JObject json = JObject.Parse(document);

            var tokens = json.SelectTokens(@"_id.$oid");
            if (tokens.Count() == 0)
            {
                return json["_id"].ToString();
            }
            else
            {
                return tokens.FirstOrDefault().Value<string>();
            }
        }

        private IActionResult BadRequestDetail(string message)
        {
            return StatusCode(400, new ProblemDetails() 
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = message
            });
        }

        private IActionResult ObjectNotFound(string id, string collectionName)
        {
            return StatusCode(404, new ProblemDetails() 
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Object '{id}' does not exist in collection '{collectionName}'" 
            });
        }

        private IActionResult CollectionNotFound(string collectionName)
        {
            return StatusCode(404, new ProblemDetails() 
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Collection '{collectionName}' does not exist" 
            });
        }
    }
}
