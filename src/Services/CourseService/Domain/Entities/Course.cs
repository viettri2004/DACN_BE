using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Domain.Entities;
using CourseService.Domain.Enums;

namespace Entities
{
    public class Course
    {
        public string Id { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string? ImagePublicId { get; set; } 
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string Description { get; set; } = null!;
        public CourseStatus Status { get; set; } = CourseStatus.Private;
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "All Levels";
        public string Language { get; set; } = "Vietnamese";
        public string Access { get; set; } = "Lifetime";
        public string InstructorId { get; set; } = null!;
        public Instructor Instructor { get; set; } = null!;
        public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
        //public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
        //public ICollection<LeaveComment> LeaveComments { get; set; } = new List<LeaveComment>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<CourseRequest> CourseRequests { get; set; } = new List<CourseRequest>();
    }
}