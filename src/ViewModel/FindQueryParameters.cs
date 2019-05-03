using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.ObjectService.ViewModel
{
    /// <summary>
    /// Class representing query parameters for the find operation
    /// </summary>
    public class FindQueryParameters
    {
        /// <summary>
        /// The start point for the find operation
        /// </summary>
        /// <example>10</example>
        [Range(0, Int32.MaxValue)]
        [FromQuery(Name = "start")]
        public int Start { get; set; }

        /// <summary>
        /// The size of the collection that is returned to the client
        /// </summary>
        /// <example>50</example>
        [Range(-1, Int32.MaxValue)]
        [FromQuery(Name = "size")]
        public int Limit { get; set; }

        /// <summary>
        /// Name of the Json property that will be used to sort the returned collection
        /// </summary>
        /// <example>age</example>
        [StringLength(1000)]
        [RegularExpression(@"^[a-zA-Z0-9]*$")]
        [FromQuery(Name = "sort")]
        public string SortFieldName { get; set; }

        /// <summary>
        /// The sort order; use 1 for ascending and -1 for descending
        /// </summary>
        /// <example>1</example>
        [Range(-1, 1)]
        [FromQuery(Name = "order")]
        public int Order { get; set; }
    }
}