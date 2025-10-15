using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Course
    {
        public string Id { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string Description { get; set; } = null!;
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
        public string InstructorId { get; set; } = null!;
        public Instructor Instructor { get; set; } = null!;
        public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
        //public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
        //public ICollection<LeaveComment> LeaveComments { get; set; } = new List<LeaveComment>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}