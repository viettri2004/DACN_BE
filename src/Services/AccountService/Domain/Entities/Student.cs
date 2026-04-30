namespace Entities
{
    public class Student : User
    {
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}