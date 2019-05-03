using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core;

using Foundation.ObjectService.Data;

namespace Foundation.ObjectService.WebUI
{
    /// <summary>
    /// Health check for a NoSQL database
    /// </summary>
    public class ObjectDatabaseHealthCheck : IHealthCheck
    {
        private readonly int _degradationThreshold;
        private readonly int _cancellationThreshold;
        private readonly string _description;
        private readonly IObjectRepository _repository = null;
        private const string DUMMY_DB_NAME = "_healthcheckdatabase_";
        private const string DUMMY_COLLECTION_NAME = "_healthcheckcollection_";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">Description of the health check</param>
        /// <param name="repository">The NoSQL object repository to use for the check</param>
        /// <param name="degradationThreshold">The threshold in milliseconds after which to consider the database degraded</param>
        /// <param name="cancellationThreshold">The threshold in milliseconds after which to cancel the check and consider the database unavailable</param>
        public ObjectDatabaseHealthCheck(string description, IObjectRepository repository, int degradationThreshold = 1000, int cancellationThreshold = 2000)
        {
            #region Input validation
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (degradationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(degradationThreshold));
            }
            if (cancellationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cancellationThreshold));
            }
            #endregion // Input validation

            _description = description;
            _repository = repository;
            _degradationThreshold = degradationThreshold;
            _cancellationThreshold = cancellationThreshold;
        }

        /// <summary>
        /// Checks the health of the database
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HealthCheckResult</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_cancellationThreshold);

            var t = Task.Factory.StartNew( async () =>
            {
                var checkResult = HealthCheckResult.Unhealthy();

                try 
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var deleteResult = await _repository.DeleteAsync(DUMMY_DB_NAME, DUMMY_COLLECTION_NAME, 1);
                    var insertResult = await _repository.InsertAsync(DUMMY_DB_NAME, DUMMY_COLLECTION_NAME, 1, "{ 'name' : 'the nameless ones' }");
                    var getResult = await _repository.GetAsync(DUMMY_DB_NAME, DUMMY_COLLECTION_NAME, 1);

                    sw.Stop();
                    var elapsed = sw.Elapsed.TotalMilliseconds.ToString("N0");

                    if (sw.Elapsed.TotalMilliseconds > _degradationThreshold)
                    {
                        checkResult = HealthCheckResult.Degraded(
                            data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                            description: $"{_description} liveness check took more than {_degradationThreshold} milliseconds");
                    }
                    else 
                    {
                        checkResult = HealthCheckResult.Healthy(
                            data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                            description: $"{_description} liveness check completed in {elapsed} milliseconds");
                    }
                }
                catch (Exception ex)
                {
                    checkResult = HealthCheckResult.Unhealthy(
                        data: new Dictionary<string, object> { ["exceptionType"] = ex.GetType().ToString() },
                        description: $"{_description} liveness check failed due to exception");
                }

                return checkResult;

            }, cancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();

            try 
            {
                t.Wait(cancellationTokenSource.Token);
            }
            catch (System.OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy(
                    description: $"{_description} liveness check canceled by server for taking more than {_cancellationThreshold} milliseconds");
            }

            return await t;
        }
    }
}