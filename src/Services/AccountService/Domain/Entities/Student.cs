namespace Entities
{
    public class Student : User
    {
        public Cart? Cart { get; set; } 
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}