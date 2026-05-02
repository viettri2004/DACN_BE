using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using Entities;
using Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CourseService.Infrastructure.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly AppDbContext _context;

        public WishlistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wishlist?> GetWishlistItemAsync(string studentId, string courseId)
        {
            return await _context.Wishlists.FirstOrDefaultAsync(w => w.StudentId == studentId && w.CourseId == courseId);
        }

        public async Task AddAsync(Wishlist wishlist)
        {
            await _context.Wishlists.AddAsync(wishlist);
        }

        public async Task RemoveAsync(Wishlist wishlist)
        {
            _context.Wishlists.Remove(wishlist);
            await Task.CompletedTask;
        }

        public IQueryable<Wishlist> GetWishlistQueryable()
        {
            return _context.Wishlists;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
