using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Swashbuckle.AspNetCore.Swagger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Driver;
using Foundation.ObjectService.Data;
using Foundation.ObjectService.Security;

namespace Foundation.ObjectService.WebUI
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string authorizationDomain = Common.GetConfigurationVariable(Configuration, "OAUTH2_ACCESS_TOKEN_URI", "Auth:Domain", string.Empty);
            bool useAuthorization = !string.IsNullOrEmpty(authorizationDomain);
            
            services.AddSwaggerGen(c =>
            {
                #region Swagger generation
                c.SwaggerDoc("v1", new Info
                    {
                        Title = "FDNS Object Microservice API",
                        Version = "v1",
                        Description = "A microservice for providing an abstraction layer to a database engine, where HTTP actions are mapped to CRUD operations. Clients of the object service and the underlying database technology may thus change independent of one another provided the API remains consistent.",
                        Contact = new Contact
                        {
                            Name = "Erik Knudsen",
                            Email = string.Empty,
                            Url = "https://github.com/erik1066"
                        },
                        License = new License
                        {
                            Name = "Apache 2.0",
                            Url = "https://www.apache.org/licenses/LICENSE-2.0"
                        }
                    }
                );

                if (useAuthorization)
                {
                    c.AddSecurityDefinition("Bearer", new ApiKeyScheme { In = "header", Description = "Please enter JWT with Bearer into field", Name = "Authorization", Type = "apiKey" });
                    c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                        { "Bearer", Enumerable.Empty<string>() },
                    });
                }

                // These two lines are necessary for Swagger to pick up the C# XML comments and show them in the Swagger UI. See https://github.com/domaindrivendev/Swashbuckle.AspNetCore for more details.
                var filePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "api.xml");
                c.IncludeXmlComments(filePath);
                #endregion
            });

            services.AddMvc(options =>
            {
               options.InputFormatters.Insert(0, new TextPlainInputFormatter());
               options.InputFormatters.Insert(0, new JsonRawInputFormatter());
               options.OutputFormatters.Insert(0, new JsonRawOutputFormatter());
            })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            var mongoConnectionString = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_CONNECTION_STRING", "MongoDB:ConnectionString", "mongodb://localhost:27017");
            string mongoUseSsl = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_USE_SSL", "MongoDB:UseSsl", "false");

            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(mongoConnectionString));

            if (mongoUseSsl.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            }

            services.AddSingleton<IMongoClient>(provider => new MongoClient(settings));
            services.AddSingleton<IObjectRepository>(provider => new MongoRepository(provider.GetService<IMongoClient>(), provider.GetService<ILogger<MongoRepository>>(), GetImmutableCollections()));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = authorizationDomain;
                options.Audience = Common.GetConfigurationVariable(Configuration, "OAUTH2_CLIENT_ID", "Auth:ApiIdentifier", string.Empty);
            });

            services.AddHealthChecks()
                .AddCheck<IHealthCheck>("database", null, new List<string> { "ready", "mongo", "db" });

            services.AddSingleton<IHealthCheck>(provider => new ObjectDatabaseHealthCheck("Database", provider.GetService<IObjectRepository>()));

            /* These policy names match the names in the [Authorize] attribute(s) in the Controller classes.
             * The HasScopeHandler class is used (see below) to pass/fail the authorization check if authorization
             * has been enabled via the microservice's configuration.
             */
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Common.READ_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.READ_AUTHORIZATION_NAME, authorizationDomain)));
                options.AddPolicy(Common.INSERT_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.INSERT_AUTHORIZATION_NAME, authorizationDomain)));
                options.AddPolicy(Common.UPDATE_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.UPDATE_AUTHORIZATION_NAME, authorizationDomain)));
                options.AddPolicy(Common.DELETE_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.DELETE_AUTHORIZATION_NAME, authorizationDomain)));
            });

            // If the developer has not configured OAuth2, then disable authentication and authorization
            if (useAuthorization)
            {
                services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
            }
            else
            {
                services.AddSingleton<IAuthorizationHandler, AlwaysAllowHandler>();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                await next();
            });

            app.UseCors("CorsPolicy");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseDefaultFiles();
            app.UseStaticFiles();
           
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FDNS Object Microservice API V1");
            });

            app.UseHealthChecks("/health/live", new HealthCheckOptions
            {
                // Exclude all checks, just return a 200.
                Predicate = (check) => false
            });

            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = (check) => check.Tags.Contains("ready"),
                ResponseWriter = WriteResponse
            });

            app.UseAuthentication();

            app.UseMvc();
        }

        private static Task WriteResponse(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(p => new JProperty(p.Key, p.Value))))))))));
            return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        }

        private Dictionary<string, HashSet<string>> GetImmutableCollections()
        {
            var immutableCollection = new Dictionary<string, HashSet<string>>();

            var immutableCollectionsStr = Common.GetConfigurationVariable(Configuration, "OBJECT_IMMUTABLE", "MongoDB:Immutable", string.Empty);
            if (!string.IsNullOrEmpty(immutableCollectionsStr))
            {
                string [] immutableCollections = immutableCollectionsStr.Split(';');
                foreach (var entry in immutableCollections)
                {
                    var parts = entry.Split('/');
                    if (parts.Length == 2)
                    {
                        var databaseName = parts[0];
                        var collectionName = parts[1];

                        if (!immutableCollection.ContainsKey(databaseName))
                        {
                            immutableCollection.Add(databaseName, new HashSet<string>());
                        }
                        if (!immutableCollection[databaseName].Contains(collectionName))
                        {
                            immutableCollection[databaseName].Add(collectionName);
                        }
                    }
                }
            }
            return immutableCollection;
        }
    }
#pragma warning restore 1591
}