using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.ObjectService.ViewModel
{
    /// <summary>
    /// Class representing route parameters for the object service
    /// </summary>
    public class DatabaseRouteParameters
    {
        /// <summary>
        /// The name of the database to use
        /// </summary>
        /// <example>bookstore</example>
        [Required]
        [StringLength(250)]
        [RegularExpression(@"^[a-zA-Z0-9\/_]*$")]
        [FromRoute(Name = "db")]
        public string DatabaseName { get; set; }

        /// <summary>
        /// The name of the collection within the database to use
        /// </summary>
        /// <example>customer</example>
        [Required]
        [StringLength(250)]
        [RegularExpression(@"^[a-zA-Z0-9\/_]*$")]
        [FromRoute(Name = "collection")]
        public string CollectionName { get; set; }
    }
}