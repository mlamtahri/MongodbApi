using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.ObjectService.Security
{
    /// <summary>
    /// Class for handling scope requirements specific to the foundation services scoping authorization model
    /// </summary>
    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        private const string SCOPE = "scope";

        private string GetScopeFromRoute(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext resource)
        {
            int dbIndex = 0;
            int collectionIndex = 0;
            int i = 0;
            foreach (var key in resource.RouteData.Values.Keys)
            {
                if (key == "db")
                {
                    dbIndex = i;
                }
                else if (key == "collection")
                {
                    collectionIndex = i;
                }
                i++;
            }

            var db = string.Empty;
            var collection = string.Empty;
            i = 0;
            foreach (var value in resource.RouteData.Values.Values)
            {
                if (i == dbIndex)
                {
                    db = value.ToString();
                }
                if (i == collectionIndex)
                {
                    collection = value.ToString();
                }
                i++;
            }

            var scope = $"fdns.object.{db}.{collection}";
            return scope;
        }

        /// <summary>
        /// Determine if the user's scope claim (if any) matches the URL and HTTP operation they are attempting to carry out
        /// </summary>
        /// <param name="context">Contains authorization information used by Microsoft.AspNetCore.Authorization.IAuthorizationHandler</param>
        /// <param name="requirement">Information about what the requirement for this HTTP operation are</param>
        /// <returns>Task</returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            // Let's see if the resource is an auth filter. If not, exit
            var resource = (context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext);
            if (resource == null || !resource.RouteData.Values.Keys.Contains("db") || !resource.RouteData.Values.Keys.Contains("collection"))
            {
                return Task.CompletedTask;
            }

            /* We need to get the dot-separated path to the collection, such as fdns.object.bookstore.customer. This dot-separated
             * path is mapped to an HTTP route: "object" is the name of the servce (the Object microservice), "bookstore" is
             * the database name, and "customer" is the collection, e.g. /api/1.0/bookstore/customer. Before we can authorize
             * the user we have to build that dot-separated list so we can compare it to one of the scopes that was passed in
             * via the OAuth2 token. The first step is to get the dot-separated list from the URL, and then we add the
             * create/read/update/delete/etc portion at the end, per the requirement passed into the method call.
             */
            var scope = $"{GetScopeFromRoute(resource)}.{requirement.Scope}";

            // Just a check to see if the user identity object has a scope claim. If not, something is wrong and exit
            if (!context.User.HasClaim(c => c.Type == SCOPE && c.Issuer == requirement.Issuer))
            {
                return Task.CompletedTask;
            }

            /* Let's figure out all the scopes the user has been authorized to. These came from the OAuth2 token and have been
             * parsed by the ASP.NET Core middleware. We just an array of strings for simplicity's sake.
             */
            var scopes = context.User.FindFirst(c => c.Type == SCOPE && c.Issuer == requirement.Issuer).Value.Split(' ');

            // Succeed if the scope array contains the required scope
            if (scopes.Any(s => s == scope))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}