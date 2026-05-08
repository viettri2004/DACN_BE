using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Domain.Enums;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using InteractionService.Application.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using InteractionService.Domain.Entities;

namespace InteractionService.Application.Interfaces
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



