using System.Linq;
using System.Threading.Tasks;
using CourseService.Domain.Entities;
using Entities;

namespace CourseService.Application.Interfaces
{
    public interface IWishlistRepository
    {
        Task<Wishlist?> GetWishlistItemAsync(string studentId, string courseId);
        Task AddAsync(Wishlist wishlist);
        Task RemoveAsync(Wishlist wishlist);
        IQueryable<Wishlist> GetWishlistQueryable();
        Task SaveChangesAsync();
    }
}
