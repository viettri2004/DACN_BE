using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class StudentCourse
    {
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;

        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; } = 0m;

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime ExpireTime { get; set; } = DateTime.UtcNow.AddYears(1);
    }
}