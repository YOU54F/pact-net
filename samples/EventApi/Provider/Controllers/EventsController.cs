using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Provider.Api.Web.Models;

namespace Provider.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        [HttpPost]
        [Route("upload-file")]
        [Consumes("multipart/form-data")]
        public IActionResult FileUpload()
        {
            var singleFile = Request.Form.Files.SingleOrDefault(f => f.Name == "file");
            if (singleFile == null || Request.Form.Files.Count != 1)
            {
                return BadRequest("Request must contain a single file with a parameter named 'file'");
            }
            if (singleFile.ContentType != "image/jpeg")
            {
                return BadRequest("File content-type must be image/jpeg");
            }
            return StatusCode(201);
        }

    }
}
