using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CourseService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
{
    public interface ITagRepository
    {
        Task<ApiResponse> CreateTagAsync(CreateTagDTO createTagDTO);
        Task<ApiResponse> DeleteTagAsync(string tagId);
        Task<ApiResponse> GetAllTagsAsync();
        //Task<ApiResponse> GetTagByIdAsync(string tagId);
        Task<ApiResponse> AssignTagToCourseAsync(AssignTagToCourseDTO assignTagToCourseDTO);
    }
}