using Data.Context;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using System.Security.Cryptography;

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
            await _context.Database.MigrateAsync();

            // await SeedRolesAsync();
            // // Bước 1: Chỉ tạo user cơ bản
            // await SeedInstructorsAsync();
            // await SeedStudentsAsync();

            // Bước 2: Dùng hàm mới để cập nhật thông tin cho user có sẵn
            await SeedUserProfilesAsync();

            // await SeedCoursesAsync();
            // await SeedTagsAsync();
            // await SeedCourseTagsAsync();
            // await SeedEnrollmentsAndCommentsAsync();
            await SeedCourseContentAsync();

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

        /// <summary>
        /// Hàm này chỉ tạo các tài khoản Instructor cơ bản.
        /// </summary>
        // private async Task SeedInstructorsAsync()
        // {
        //     if (await _context.Users.OfType<Instructor>().AnyAsync()) return;
            
        //     var instructorsToCreate = new List<RegisterDTO>
        //     {
        //         new() { UserName = "john_dev", Email = "john.dev@example.com", Password = "Password123@", FullName = "John Dev", PhoneNumber = "0901112231", Role = "Instructor" },
        //         new() { UserName = "jane_data", Email = "jane.data@example.com", Password = "Password123@", FullName = "Jane Data", PhoneNumber = "0901112232", Role = "Instructor" },
        //         new() { UserName = "mike_uiux", Email = "mike.uiux@example.com", Password = "Password123@", FullName = "Mike UI/UX", PhoneNumber = "0901112233", Role = "Instructor" },
        //         new() { UserName = "emily_agile", Email = "emily.agile@example.com", Password = "Password123@", FullName = "Emily Agile", PhoneNumber = "0901112234", Role = "Instructor" },
        //         new() { UserName = "david_sec", Email = "david.sec@example.com", Password = "Password123@", FullName = "David Security", PhoneNumber = "0901112235", Role = "Instructor" }
        //     };

        //     foreach (var dto in instructorsToCreate)
        //     {
        //         var result = await _accountService.Register(dto);
        //         if (result.Success)
        //             Console.WriteLine($"Created instructor account: {dto.UserName}");
        //         else
        //             Console.WriteLine($"Failed to create instructor {dto.UserName}: {result.Message}");
        //     }
        // }

        /// <summary>
        /// Hàm này chỉ tạo các tài khoản Student cơ bản.
        /// </summary>
        // private async Task SeedStudentsAsync()
        // {
        //     if (await _context.Users.OfType<Student>().AnyAsync()) return;

        //     for (int i = 1; i <= 10; i++)
        //     {
        //         var registerDto = new RegisterDTO
        //         {
        //             UserName = $"student{i}",
        //             Email = $"student{i}@example.com",
        //             Password = "Password123@",
        //             FullName = $"Student {i}",
        //             PhoneNumber = $"09100000{i:D2}",
        //             Role = "Student"
        //         };

        //         var result = await _accountService.Register(registerDto);
        //         if (result.Success)
        //             Console.WriteLine($"Created student account: {registerDto.UserName}");
        //         else
        //             Console.WriteLine($"Failed to create student {registerDto.UserName}: {result.Message}");
        //     }
        // }
        
        // --- CÁC HÀM MỚI ĐỂ CẬP NHẬT USER CÓ SẴN ---

        /// <summary>
        /// Hàm chính để điều phối việc cập nhật thông tin chi tiết cho các user.
        /// </summary>
        public async Task SeedUserProfilesAsync()
        {
            await UpdateInstructorProfilesAsync();
            await UpdateStudentProfilesAsync();
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết cho các Instructor chưa có JobPosition.
        /// </summary>
        private async Task UpdateInstructorProfilesAsync()
        {
            var instructorsToUpdate = await _context.Users.OfType<Instructor>()
                .Where(u => string.IsNullOrEmpty(u.JobPosition)).ToListAsync();
            
            if (!instructorsToUpdate.Any()) return;

            var profiles = new List<(string job, string org, string avatarSeed, string desc)>
            {
                ("Software Architect", "Google", "developer", "Kiến trúc sư phần mềm với 10 năm kinh nghiệm xây dựng các hệ thống scalable."),
                ("Data Scientist", "Facebook", "scientist", "Chuyên gia về Khoa học dữ liệu và Học máy, có kinh nghiệm triển khai nhiều mô hình AI."),
                ("Lead UI/UX Designer", "Amazon", "designer", "Dẫn đầu đội ngũ thiết kế, chuyên sâu về trải nghiệm người dùng và thiết kế hiện đại."),
                ("Agile Coach", "Microsoft", "agile-coach", "Huấn luyện viên Agile giúp các đội nhóm làm việc hiệu quả và linh hoạt."),
                ("Security Expert", "Netflix", "security", "Chuyên gia bảo mật ứng dụng web với kinh nghiệm phát hiện và vá các lỗ hổng hệ thống.")
            };

            for (int i = 0; i < instructorsToUpdate.Count; i++)
            {
                var user = instructorsToUpdate[i];
                var profile = profiles[i % profiles.Count]; // Dùng toán tử chia lấy dư để lặp lại profile nếu thiếu

                user.JobPosition = profile.job;
                user.Organization = profile.org;
                user.Description = profile.desc;
                user.AvatarUrl = $"https://i.pravatar.cc/150?u={profile.avatarSeed}";
                
                await _userManager.UpdateAsync(user);
            }
            
            Console.WriteLine($"Updated profiles for {instructorsToUpdate.Count} instructors.");
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết cho các Student chưa có JobPosition.
        /// </summary>
        private async Task UpdateStudentProfilesAsync()
        {
            var studentsToUpdate = await _context.Users.OfType<Student>()
                .Where(u => string.IsNullOrEmpty(u.JobPosition)).ToListAsync();

            if (!studentsToUpdate.Any()) return;

            var random = new Random();
            int studentCounter = 1;
            foreach (var user in studentsToUpdate)
            {
                bool isUniversityStudent = random.Next(0, 2) == 1;
                if (isUniversityStudent)
                {
                    user.JobPosition = "Sinh viên";
                    user.Organization = "Đại học Bách Khoa (BKU)";
                }
                else
                {
                    user.JobPosition = "Học sinh";
                    user.Organization = "Trường THPT ABC";
                }
                user.Description = "Đam mê học hỏi và khám phá các công nghệ mới.";
                user.AvatarUrl = $"https://i.pravatar.cc/150?u=student{studentCounter++}";
                
                await _userManager.UpdateAsync(user);
            }

            Console.WriteLine($"Updated profiles for {studentsToUpdate.Count} students.");
        }

        // --- CÁC PHƯƠNG THỨC KHÁC GIỮ NGUYÊN ---
        
        private async Task SeedCoursesAsync()
        {
            if (await _context.Courses.AnyAsync()) return;

            var instructors = await _context.Users.OfType<Instructor>().ToListAsync();
            if (!instructors.Any())
            {
                Console.WriteLine("No instructors found to assign courses.");
                return;
            }

            var random = new Random();

            var courseData = new List<(string Name, string Description, string ImageSeed)>
            {
                ("Lập trình C# từ Zero đến Hero", "Khóa học toàn diện về C#, từ cơ bản đến nâng cao như OOP, LINQ, và Async/Await. Lý tưởng cho người mới bắt đầu.", "CSharp"),
                ("Phát triển Web API với ASP.NET Core", "Học cách xây dựng các API RESTful mạnh mẽ, bảo mật và hiệu quả bằng ASP.NET Core.", "WebAPI"),
                ("Mastering ReactJS và Redux", "Làm chủ thư viện frontend phổ biến nhất thế giới để xây dựng các ứng dụng web động và hiệu năng cao.", "React"),
                ("Xây dựng ứng dụng Full-Stack với Angular và .NET", "Kết hợp sức mạnh của Angular và .NET để tạo ra các ứng dụng web hoàn chỉnh, từ giao diện đến cơ sở dữ liệu.", "FullStack"),
                ("Thiết kế và Tối ưu Cơ sở dữ liệu SQL", "Tìm hiểu các nguyên tắc thiết kế database, viết truy vấn SQL hiệu quả và các kỹ thuật tối ưu hóa hiệu suất.", "Database"),
                ("Nhập môn DevOps: CI/CD với GitHub Actions", "Tự động hóa quy trình xây dựng, kiểm thử và triển khai phần mềm với các pipeline CI/CD sử dụng GitHub Actions.", "DevOps"),
                ("Kiến trúc Microservices cho người bắt đầu", "Khám phá các khái niệm, ưu và nhược điểm của kiến trúc microservices và cách triển khai chúng.", "Microservices"),
                ("Làm chủ Docker và Kubernetes", "Học cách container hóa ứng dụng với Docker và điều phối chúng trên một cụm với Kubernetes.", "Kubernetes"),
                ("Lập trình hướng đối tượng (OOP) chuyên sâu", "Đi sâu vào 4 nguyên tắc của OOP và các mẫu thiết kế (Design Patterns) để viết mã có tổ chức và dễ bảo trì.", "OOP"),
                ("Thiết kế UI/UX cho ứng dụng di động", "Nắm vững các nguyên tắc thiết kế giao diện và trải nghiệm người dùng để tạo ra các ứng dụng di động đẹp và thân thiện.", "UIUX"),
                ("Giới thiệu về Machine Learning và Python", "Bước đầu vào thế giới học máy, tìm hiểu các thuật toán phổ biến và xây dựng mô hình dự đoán đầu tiên với Python.", "MachineLearning"),
                ("Quản lý dự án phần mềm với Agile và Scrum", "Học cách quản lý các dự án phần mềm một cách linh hoạt, hiệu quả và thích ứng nhanh với thay đổi.", "Agile"),
                ("Lập trình Front-end nâng cao với TypeScript", "Nâng cao kỹ năng JavaScript của bạn bằng cách sử dụng TypeScript để viết mã an toàn hơn và có cấu trúc rõ ràng.", "TypeScript"),
                ("Xây dựng Real-time Application với SignalR", "Thêm các tính năng thời gian thực như chat, thông báo đẩy vào ứng dụng .NET của bạn một cách dễ dàng với SignalR.", "Realtime"),
                ("Bảo mật ứng dụng Web: Từ lý thuyết đến thực hành", "Tìm hiểu về các lỗ hổng bảo mật web phổ biến (OWASP Top 10) và cách phòng chống chúng.", "Security"),
                ("Nhập môn Điện toán đám mây với Azure", "Khám phá các dịch vụ cốt lõi của Microsoft Azure như Virtual Machines, App Service và SQL Database.", "Azure"),
                ("Entity Framework Core Toàn tập", "Làm chủ ORM mạnh mẽ của .NET để tương tác với cơ sở dữ liệu một cách hiệu quả và tự nhiên.", "EntityFramework"),
                ("Thiết kế hệ thống (System Design) cho phỏng vấn", "Chuẩn bị cho các cuộc phỏng vấn kỹ thuật bằng cách học cách thiết kế các hệ thống quy mô lớn như mạng xã hội, dịch vụ streaming.", "SystemDesign"),
                ("Lập trình bất đồng bộ trong C# (Async/Await)", "Hiểu sâu về cách hoạt động của async/await để xây dựng các ứng dụng có độ phản hồi cao và không bị block.", "Async"),
                ("Test tự động: Unit Test và Integration Test trong .NET", "Học cách viết các bài test tự động để đảm bảo chất lượng code và giảm thiểu lỗi trong quá trình phát triển.", "Testing")
            };

            var courses = new List<Course>();
            foreach (var data in courseData)
            {
                var instructor = instructors[random.Next(instructors.Count)];
                courses.Add(new Course
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = data.Name,
                    Description = data.Description,
                    Price = new decimal(random.Next(50, 250) * 10000),
                    ImageUrl = $"https://picsum.photos/seed/{data.ImageSeed}/400/300",
                    CreateTime = DateTime.UtcNow.AddDays(-random.Next(1, 100)),
                    InstructorId = instructor.Id
                });
            }
            
            await _context.Courses.AddRangeAsync(courses);
            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 20 meaningful courses.");
        }

        private async Task SeedTagsAsync()
        {
            if (await _context.Tags.AnyAsync()) return;

            var tags = new List<Tag>
            {
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Web Development", Description = "Lập trình web" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Backend", Description = "Phát triển phía server" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Frontend", Description = "Phát triển phía client" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "C#", Description = "Ngôn ngữ lập trình C#" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = ".NET", Description = "Nền tảng .NET" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "React", Description = "Thư viện JavaScript React" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Angular", Description = "Framework JavaScript Angular" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Database", Description = "Cơ sở dữ liệu" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "DevOps", Description = "Quy trình phát triển và vận hành" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Cloud Computing", Description = "Điện toán đám mây" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "UI/UX", Description = "Thiết kế giao diện và trải nghiệm người dùng" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Project Management", Description = "Quản lý dự án" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Data Science", Description = "Khoa học dữ liệu" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Machine Learning", Description = "Học máy" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Mobile Development", Description = "Phát triển ứng dụng di động" }
            };

            await _context.Tags.AddRangeAsync(tags);
            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 15 tags.");
        }

        private async Task SeedCourseTagsAsync()
        {
            if (await _context.CourseTags.AnyAsync()) return;
            
            var courses = await _context.Courses.ToListAsync();
            var tags = await _context.Tags.ToListAsync();

            if (!courses.Any() || !tags.Any())
            {
                Console.WriteLine("No courses or tags to seed relationships.");
                return;
            }

            var random = new Random();
            var courseTags = new List<CourseTag>();

            foreach (var course in courses)
            {
                var selectedTags = tags.OrderBy(t => random.Next()).Take(5).ToList();
                foreach (var tag in selectedTags)
                {
                    courseTags.Add(new CourseTag { CourseId = course.Id, TagId = tag.Id });
                }
            }

            await _context.CourseTags.AddRangeAsync(courseTags);
            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 5 random tags for each course.");
        }

        private async Task SeedEnrollmentsAndCommentsAsync()
        {
            if (await _context.Comments.AnyAsync())
            {
                Console.WriteLine("Comments already seeded.");
                return;
            }

            var courses = await _context.Courses.ToListAsync();
            var students = await _context.Users.OfType<Student>().ToListAsync();
            
            if (!courses.Any() || !students.Any())
            {
                Console.WriteLine("No courses or students available to seed comments.");
                return;
            }

            var newOrders = new List<Order>();
            var newOrderItems = new List<OrderItem>();
            var newEnrollments = new List<Enrollment>();
            var newComments = new List<Comment>();
            
            var existingEnrollments = await _context.Enrollments
                .ToDictionaryAsync(e => (e.StudentId, e.CourseId), e => e);
            
            var random = new Random();

            foreach (var course in courses)
            {
                var studentsForCourse = students.OrderBy(s => random.Next()).Take(5).ToList();
                
                foreach (var student in studentsForCourse)
                {
                    if (!existingEnrollments.TryGetValue((student.Id, course.Id), out var enrollment))
                    {
                        var order = new Order
                        {
                            Id = Guid.NewGuid().ToString(),
                            StudentId = student.Id,
                            TotalAmount = course.Price,
                            Status = "Paid",
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(5, 40)),
                            PaidAt = DateTime.UtcNow.AddDays(-random.Next(1, 5))
                        };
                        newOrders.Add(order);

                        var orderItem = new OrderItem
                        {
                            Id = Guid.NewGuid().ToString(),
                            OrderId = order.Id,
                            CourseId = course.Id,
                            Price = course.Price,
                            FinalPrice = course.Price
                        };
                        newOrderItems.Add(orderItem);

                        enrollment = new Enrollment
                        {
                            Id = Guid.NewGuid().ToString(),
                            StudentId = student.Id,
                            CourseId = course.Id,
                            OrderId = order.Id,
                            EnrolledAt = order.CreatedAt,
                            ExpiresAt = order.CreatedAt.AddYears(1),
                            Status = true
                        };
                        newEnrollments.Add(enrollment);
                        
                        existingEnrollments[(student.Id, course.Id)] = enrollment;
                    }

                    var comment = new Comment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = GetRandomComment(random),
                        Rate = random.Next(3, 6),
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 20)),
                        EnrollmentId = enrollment.Id
                    };
                    newComments.Add(comment);
                }
            }

            if (newOrders.Any()) await _context.Orders.AddRangeAsync(newOrders);
            if (newOrderItems.Any()) await _context.OrderItems.AddRangeAsync(newOrderItems);
            if (newEnrollments.Any()) await _context.Enrollments.AddRangeAsync(newEnrollments);
            if (newComments.Any()) await _context.Comments.AddRangeAsync(newComments);

            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Seeded {newComments.Count} comments successfully!");
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

        private async Task SeedCourseContentAsync()
        {
            if (await _context.Lectures.AnyAsync())
            {
                Console.WriteLine("Lectures, videos, and quizzes are already seeded.");
                return;
            }

            var courses = await _context.Courses.ToListAsync();
            if (!courses.Any())
            {
                Console.WriteLine("No courses found to seed content.");
                return;
            }

            var newLectures = new List<Lecture>();
            var newLectureVideos = new List<LectureVideo>();
            var newQuizzes = new List<Quiz>();
            var newDocuments = new List<Document>();
            
            foreach (var course in courses)
            {
                for (int i = 1; i <= 6; i++)
                {
                    var lecture = new Lecture
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Chương {i}: Giới thiệu về chủ đề",
                        Description = $"Nội dung chi tiết cho chương {i} của khóa học.",
                        CourseId = course.Id
                    };
                    newLectures.Add(lecture);

                    for (int j = 1; j <= 3; j++)
                    {
                        var lectureVideo = new LectureVideo
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Video {j}: Bài học phần {j}",
                            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ", // Placeholder
                            LectureId = lecture.Id
                        };
                        newLectureVideos.Add(lectureVideo);
                    }

                    for (int k = 1; k <= 2; k++)
                    {
                        var document = new Document
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Tài liệu tham khảo {k}",
                            LectureId = lecture.Id,
                            Type = "pdf",
                            
                        };
                        newDocuments.Add(document);
                    }

                    var quiz = new Quiz
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Bài kiểm tra cuối chương {i}",
                        LectureId = lecture.Id,
                        TestTime = 15,
                        AttemptCount = 3
                    };
                    newQuizzes.Add(quiz);
                }
            }

            await _context.Lectures.AddRangeAsync(newLectures);
            await _context.LectureVideos.AddRangeAsync(newLectureVideos);
            await _context.Quizzes.AddRangeAsync(newQuizzes);
            await _context.Documents.AddRangeAsync(newDocuments);

            await _context.SaveChangesAsync();

            Console.WriteLine($"Seeded {newLectures.Count} lectures, {newLectureVideos.Count} videos, {newQuizzes.Count} quizzes, and {newDocuments.Count} documents.");
        }
    }
}