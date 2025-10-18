using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CartService.Application.DTOs;
using CartService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;

namespace CartService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _cartRepository;
        public CartController(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        [Authorize(Policy = "Student")]
        [HttpGet("cart-items")]
        public async Task<ActionResult<ApiResponse>> GetMyCart()
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _cartRepository.GetAllItemsAsync(studentId);

            return response.ToActionResult();
        }
    
        [Authorize(Policy = "Student")]
        [HttpPost("add-course")]
        public async Task<ActionResult<ApiResponse>> AddCourseToCart([FromBody] AddToCartDTO addToCartDTO)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _cartRepository.AddToCartAsync(addToCartDTO.CourseId, studentId);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpDelete("remove-course/{courseId}")]
        public async Task<ActionResult<ApiResponse>> RemoveCourseFromCart([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _cartRepository.RemoveFromCartAsync(courseId, studentId);

            return response.ToActionResult();
        }

        
    }
}