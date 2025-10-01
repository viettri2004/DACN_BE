using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface IEmailService
    {
        Task<ApiResponse> SendEmailAsync(string toEmail);
    }
}