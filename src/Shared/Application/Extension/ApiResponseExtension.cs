using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using src.Shared.Domain.Entities;

namespace Shared.Application.Extension
{
    public static class ApiResponseExtensions
    {
        public static ActionResult<ApiResponse> ToActionResult(this ApiResponse response)
        {
            return response.Code switch
            {
                "Success" => new OkObjectResult(response),
                "Created" => new CreatedResult("", response),
                "NotFound" => new NotFoundObjectResult(response),
                "BadRequest" => new BadRequestObjectResult(response),
                "Unauthorized" => new UnauthorizedObjectResult(response),
                "Conflict" => new ConflictObjectResult(response),
                _ => new ObjectResult(response) { StatusCode = 500 } 
            };
        }
    }
}