namespace Entities
{
    public class Student : User
    {
        public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<LeaveComment> LeaveComments { get; set; } = new List<LeaveComment>();
    }
}