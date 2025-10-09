using AutoMapper;
using CourseService.Application.DTOs;
using Entities;

namespace CourseService.Application.Mappers
{
    public class CourseMapper : Profile
    {
        public CourseMapper()
        {
            CreateMap<Course, CourseListDTO>()
                .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Instructor.FullName))
                .ForMember(dest => dest.Rating, opt => opt.Ignore());

            CreateMap<Course, CourseDetailDTO>()
                .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Instructor.FullName))
                .ForMember(dest => dest.Rating, opt => opt.Ignore())
                .ForMember(dest => dest.TotalStudents, opt => opt.Ignore())
                .ForMember(dest => dest.TotalHours, opt => opt.Ignore());
            CreateMap<LeaveComment, LeaveCommentDTO>()
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.FullName));
        }
    }
}
