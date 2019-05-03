using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Foundation.ObjectService.Data;
using Foundation.ObjectService.Security;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.ObjectService
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public static class Common
    {
        public const string READ_AUTHORIZATION_NAME = "read";
        public const string INSERT_AUTHORIZATION_NAME = "insert";
        public const string UPDATE_AUTHORIZATION_NAME = "update";
        public const string DELETE_AUTHORIZATION_NAME = "delete";

        /// <summary>
        /// Gets a config value for a variable name, preferring ENV variables over appsettings variables when both are present
        /// </summary>
        /// <param name="configuration">The config object to use for pulling keys and values</param>
        /// <param name="environmentVariableName">The name of the variable to use for getting the config value</param>
        /// <param name="appSettingsVariableName">The name of the appsettings variable to use for getting the config value</param>
        /// <param name="defaultValue">The default value to use (if any) if neither config location has a value for this variable</param>
        /// <returns>string representing the config value</returns>
        public static string GetConfigurationVariable(IConfiguration configuration, string environmentVariableName, string appSettingsVariableName, string defaultValue = "")
        {
            string variableValue = string.Empty;
            if (!string.IsNullOrEmpty(appSettingsVariableName) && !string.IsNullOrEmpty(configuration[appSettingsVariableName]))
            {
                variableValue = configuration[appSettingsVariableName];
            }
            if (!string.IsNullOrEmpty(environmentVariableName) && !string.IsNullOrEmpty(configuration[environmentVariableName]))
            {
                variableValue = configuration[environmentVariableName];
            }

            if (string.IsNullOrEmpty(variableValue) && !string.IsNullOrEmpty(defaultValue))
            {
                variableValue = defaultValue;
            }

            return variableValue;
        }
    }
#pragma warning restore 1591 // disables the warnings about missing Xml code comments
}