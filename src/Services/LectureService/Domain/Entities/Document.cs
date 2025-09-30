using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Document
    {
        [Key]
        public int Id { get; set; }
        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;
        public int DocumentNumber { get; set; } = 1;

        [Required]
        public string Name { get; set; } = null!; 

        [Required]
        public string Type { get; set; } = null!; 
    }
}