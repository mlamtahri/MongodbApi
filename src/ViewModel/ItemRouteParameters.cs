using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.ObjectService.ViewModel
{
    /// <summary>
    /// Class representing route parameters for the object service
    /// </summary>
    public class ItemRouteParameters : DatabaseRouteParameters
    {
        /// <summary>
        /// The id of the item
        /// </summary>
        /// <example>939515</example>
        [Required]
        [StringLength(250)]
        [FromRoute(Name = "id")]
        public string Id { get; set; }
    }
}