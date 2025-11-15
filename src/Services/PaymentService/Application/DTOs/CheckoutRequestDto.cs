using System.Collections.Generic;

namespace PaymentService.Application.DTOs
{
    public class CheckoutRequestDto
    {
        public List<string> CourseIds { get; set; } = new List<string>();
    }
}