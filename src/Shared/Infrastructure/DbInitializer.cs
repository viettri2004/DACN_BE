using Data.Context;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;

namespace Data.Seeding
{
    public class DbSeeder
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuthService _accountService;

        public DbSeeder(
            AppDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuthService accountService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _accountService = accountService;
        }

        public async Task SeedAsync()
        {
            //await _context.Database.MigrateAsync();

            //await SeedRolesAsync();
            // await SeedInstructorsAsync();
            // await SeedStudentsAsync();
            // await SeedCoursesAsync();
            // await SeedStudentCoursesAsync();
            await SeedCourseCommentsAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Database seeding completed!");
            Console.ResetColor();
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = { "Student", "Instructor", "Admin" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"Created role: {role}");
                }
            }
        }

        private async Task SeedInstructorsAsync()
        {
            // if (_context.Users.OfType<Instructor>().Any())
            //     return;

            for (int i = 1; i <= 5; i++)
            {
                var registerDto = new RegisterDTO
                {
                    UserName = $"instructor{i}",
                    Email = $"instructor{i}@example.com",
                    Password = "P@ssword123",
                    FullName = $"Instructor {i}",
                    PhoneNumber = $"09000000{i:D2}",
                    Role = "Instructor"
                };

                var result = await _accountService.Register(registerDto);
                if (result.Success)
                    Console.WriteLine($"Seeded instructor: {registerDto.UserName}");
                else
                    Console.WriteLine($"Failed to seed instructor {registerDto.UserName}: {result.Message}");
            }
        }

        private async Task SeedStudentsAsync()
        {
            // if (_context.Users.OfType<Student>().Any())
            //     return;

            for (int i = 1; i <= 10; i++)
            {
                var registerDto = new RegisterDTO
                {
                    UserName = $"student{i}",
                    Email = $"student{i}@example.com",
                    Password = "P@ssword123",
                    FullName = $"Student {i}",
                    PhoneNumber = $"09100000{i:D2}",
                    Role = "Student"
                };

                var result = await _accountService.Register(registerDto);
                if (result.Success)
                    Console.WriteLine($"Seeded student: {registerDto.UserName}");
                else
                    Console.WriteLine($"Failed to seed student {registerDto.UserName}: {result.Message}");
            }
        }

        private async Task SeedCoursesAsync()
        {
            if (_context.Courses.Any())
                return;

            var instructors = await _context.Users.OfType<Instructor>().Take(5).ToListAsync();
            var random = new Random();

            for (int i = 1; i <= 20; i++)
            {
                var instructor = instructors[random.Next(instructors.Count)];
                _context.Courses.Add(new Course
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Course {i}",
                    Description = $"This is the description for Course {i}.",
                    CreateTime = DateTime.UtcNow.AddDays(-i),
                    InstructorId = instructor.Id
                });
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded courses");
        }

        private async Task SeedStudentCoursesAsync()
        {
            // if (_context.StudentCourses.Any())
            //     return;

            // var students = await _context.Users.OfType<Student>().ToListAsync();
            // var courses = await _context.Courses.ToListAsync();
            // var random = new Random();

            // foreach (var student in students)
            // {
            //     var selectedCourses = courses.OrderBy(c => random.Next()).Take(5).ToList();

            //     foreach (var course in selectedCourses)
            //     {
            //         _context.StudentCourses.Add(new StudentCourse
            //         {
            //             StudentId = student.Id,
            //             CourseId = course.Id,
            //             Amount = 199.99m,
            //             StartTime = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
            //             ExpireTime = DateTime.UtcNow.AddMonths(6)
            //         });
            //     }
            // }

            // await _context.SaveChangesAsync();
            // Console.WriteLine("Seeded student-course relations");
        }
        private async Task SeedCourseCommentsAsync()
        {
            // if (_context.LeaveComments.Any())
            //     return;

            // var students = await _context.Users.OfType<Student>().ToListAsync();
            // var courses = await _context.Courses.ToListAsync();
            // var random = new Random();

            // var comments = new List<LeaveComment>();

            // foreach (var course in courses)
            // {
            //     var randomStudents = students.OrderBy(x => random.Next()).Take(5).ToList();

            //     foreach (var student in randomStudents)
            //     {
            //         comments.Add(new LeaveComment
            //         {
            //             CommentId = Guid.NewGuid().ToString(),
            //             CourseId = course.Id,
            //             StudentId = student.Id,
            //             Content = GetRandomComment(random),
            //             Rate = random.Next(3, 6), // chấm 3-5 sao
            //             Timestamp = DateTime.UtcNow.AddDays(-random.Next(1, 20))
            //         });
            //     }
            // }

            // await _context.LeaveComments.AddRangeAsync(comments);
            // await _context.SaveChangesAsync();

            // Console.WriteLine("Seeded 5 comments per course successfully!");
        }
        private string GetRandomComment(Random random)
        {
            string[] comments =
            {
                "Khóa học rất hữu ích, giảng viên giảng dễ hiểu.",
                "Nội dung bài học chất lượng, đáng tiền.",
                "Mình mong có thêm ví dụ thực tế hơn.",
                "Khóa học này giúp mình nắm được kiến thức cơ bản rất nhanh.",
                "Thầy cô hỗ trợ nhiệt tình, cảm ơn nhiều!",
                "Bài giảng rõ ràng, dễ theo dõi.",
                "Khóa học nên có thêm phần luyện tập.",
                "Tổng thể khá ổn, phù hợp với người mới.",
                "Giảng viên trả lời câu hỏi rất nhanh và chi tiết.",
                "Một khóa học tuyệt vời, mình sẽ giới thiệu cho bạn bè!"
            };
            return comments[random.Next(comments.Length)];
        }
    }
}
