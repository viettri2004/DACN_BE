namespace Entities
{
    public class Instructor : User
    {
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}