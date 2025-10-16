using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Application.DTOs
{
    public class UserLearningStatsDTO
    {
        public int CompletionProgress { get; set; }
        public int TotalHours { get; set; }
        public int TotalCertificates { get; set; }
        public int CurrentStreak { get; set; }
        public double AverageGivenRating { get; set; }
    }
}