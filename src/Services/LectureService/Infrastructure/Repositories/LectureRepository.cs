using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using LectureService.Application.Interfaces;

namespace LectureService.Infrastructure.Repositories
{
    public class LectureRepository : ILectureRepository
    {
        private readonly AppDbContext _context;
        public LectureRepository(AppDbContext context)
        {
            _context = context;
        }
        
    }
}