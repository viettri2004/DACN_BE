using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;

namespace Entities
{
    public class CourseTag
    {
        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public string TagId { get; set; } = null!;
        public Tag Tag { get; set; } = null!;

    }
}