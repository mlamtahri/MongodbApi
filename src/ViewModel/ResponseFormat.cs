using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.ObjectService.ViewModel
{
    /// <summary>
    /// Enum for response body formats for returned objects
    /// </summary>
    public enum ResponseFormat
    {
        /// <summary>
        /// Return the entire object
        /// </summary>
        [Description("Entire object")]
        EntireObject,

        /// <summary>
        /// Return only the object's ID
        /// </summary>
        [Description("Id only")]
        OnlyId
    }
}