using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Foundation.ObjectService.WebUI
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class HttpHealthCheck : IHealthCheck
    {
        private readonly string _url;
        private readonly int _threshold;
        private readonly string _description;
        private readonly HttpClient _client = null; // TODO: Use DI and Polly for handling HttpClient so we can unit test it properly

        public HttpHealthCheck(string description, string url, int threshold = 1000)
        {
            #region Input validation
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (threshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold));
            }
            #endregion // Input validation

            _description = description;
            _url = url;
            _threshold = threshold;
            _client = new HttpClient();
            _client.Timeout = new TimeSpan(0, 0, 0, 5); // five-second timeout
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var checkResult = HealthCheckResult.Healthy();

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, _url))
            {
                var sw = new Stopwatch();
                sw.Start();

                try 
                {
                    using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                    {
                        var responseValue = await response.Content.ReadAsStringAsync();
                    }

                    sw.Stop();
                    var elapsed = sw.Elapsed.TotalMilliseconds.ToString("N0");

                    if (sw.Elapsed.TotalMilliseconds > _threshold)
                    {
                        checkResult = HealthCheckResult.Degraded(
                            data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                            description: $"{_description} liveness probe took more than {_threshold} milliseconds");
                    }
                    else 
                    {
                        checkResult = HealthCheckResult.Healthy(
                            data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                            description: $"{_description} liveness probe completed in {elapsed} milliseconds");
                    }
                }
                catch (Exception ex)
                {
                    checkResult = new HealthCheckResult(
                        status: context.Registration.FailureStatus,
                        description: $"{_description} unavailable", 
                        exception: ex);
                }
                finally
                {
                    sw.Stop();
                }
            }

            return checkResult;
        }
    }
#pragma warning restore 1591
}