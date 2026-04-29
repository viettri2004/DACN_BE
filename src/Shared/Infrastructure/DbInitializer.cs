using Data.Context;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CourseService.Domain.Entities;
using AccountService.Domain.Enums;
using CourseService.Domain.Enums;

namespace Data.Seeding
{
    public class DbSeeder
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Random _random = new Random();

        public DbSeeder(
            AppDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await _context.Database.MigrateAsync();

            Console.WriteLine("Starting database seeding...");

            // Phase 1: Users & Roles
            await SeedRolesAsync();
            await SeedUsersAsync();

            // Phase 2: Course Foundation
            await SeedCoursesAsync();
            await SeedTagsAsync();
            await SeedCourseTagsAsync();

            // Phase 3: Course Content
            await SeedCourseContentAsync();
            await SeedQuestionsAndOptionsAsync();

            // Phase 4: Enrollment & Payment
            await SeedEnrollmentsAndCommentsAsync();
            await SeedPaymentTransactionsAsync();
            await SeedCourseRequestsAsync();

            // Phase 5: Student Activity
            await SeedQuizAttemptsAsync();
            await SeedStudentLectureProgressAsync();

            // Phase 6: Misc
            await SeedGiftCodesAsync();
            await SeedNotificationsAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Database seeding completed successfully!");
            Console.ResetColor();
        }

        // ===== PHASE 1: ROLES & USERS =====

        private async Task SeedRolesAsync()
        {
            string[] roles = { "Student", "Instructor", "Admin" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"  Created role: {role}");
                }
            }
        }

        private async Task SeedUsersAsync()
        {
            if (await _context.Users.AnyAsync())
            {
                Console.WriteLine("Users already seeded.");
                return;
            }

            // 1. Admin (3 roles: Admin + Instructor + Student)
            var admin = new Admin
            {
                UserName = "admin",
                Email = "admin@vietedu.edu.vn",
                FullName = "System Administrator",
                PhoneNumber = "0123456789",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                JobPosition = "Quản trị viên hệ thống",
                Organization = "VietEdu Academy",
                AvatarUrl = "https://i.pravatar.cc/150?u=admin",
                Description = "Tài khoản quản trị cao cấp nhất của hệ thống."
            };
            await CreateUserWithRoles(admin, "Password123@", new[] { "Admin", "Instructor", "Student" });

            // 2. Five Instructors (2 roles: Instructor + Student)
            var instructorProfiles = new List<(string FullName, string Username, string Job, string Org, string Desc)>
            {
                ("Nguyễn Văn An", "instructor1", "Senior .NET Developer", "FPT Software", "10 năm kinh nghiệm xây dựng hệ thống enterprise với .NET."),
                ("Trần Thị Bình", "instructor2", "Frontend Tech Lead", "VNG Corporation", "Chuyên gia React/Angular, đam mê tạo UI mượt mà."),
                ("Lê Quang Cường", "instructor3", "Data Architect", "Momo", "Kiến trúc sư dữ liệu với kinh nghiệm xử lý hàng triệu giao dịch/ngày."),
                ("Phạm Hồng Duyên", "instructor4", "UI/UX Designer", "Be Group", "Thiết kế trải nghiệm người dùng cho các ứng dụng triệu người dùng."),
                ("Hoàng Anh Em", "instructor5", "AI Researcher", "VinAI", "Nghiên cứu và ứng dụng AI/ML vào các bài toán thực tế.")
            };

            foreach (var p in instructorProfiles)
            {
                var instructor = new Instructor
                {
                    UserName = p.Username,
                    Email = $"{p.Username}@vietedu.edu.vn",
                    FullName = p.FullName,
                    PhoneNumber = "09" + RandomNumberString(8),
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    JobPosition = p.Job,
                    Organization = p.Org,
                    AvatarUrl = $"https://i.pravatar.cc/150?u={p.Username}",
                    Description = p.Desc
                };
                await CreateUserWithRoles(instructor, "Password123@", new[] { "Instructor", "Student" });
            }

            // 3. Five Students (1 role: Student)
            var studentNames = new[] { "Võ Minh Khải", "Đặng Thùy Linh", "Bùi Đức Mạnh", "Ngô Thanh Nhàn", "Dương Quốc Phong" };
            for (int i = 0; i < 5; i++)
            {
                var student = new Student
                {
                    UserName = $"student{i + 1}",
                    Email = $"student{i + 1}@vietedu.edu.vn",
                    FullName = studentNames[i],
                    PhoneNumber = "08" + RandomNumberString(8),
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    JobPosition = "Sinh viên",
                    Organization = "Đại học Bách Khoa",
                    AvatarUrl = $"https://i.pravatar.cc/150?u=student{i + 1}",
                    Description = "Yêu thích lập trình và luôn mong muốn học hỏi kỹ năng mới."
                };
                await CreateUserWithRoles(student, "Password123@", new[] { "Student" });

                // Create a shopping cart for each student
                _context.Carts.Add(new Cart { Id = Guid.NewGuid().ToString(), StudentId = student.Id });
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 11 users (1 Admin, 5 Instructors, 5 Students).");
        }

        private async Task CreateUserWithRoles(User user, string password, string[] roles)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                foreach (var role in roles)
                    await _userManager.AddToRoleAsync(user, role);
                Console.WriteLine($"  Created user: {user.UserName} [{string.Join(", ", roles)}]");
            }
            else
            {
                Console.WriteLine($"  FAILED user {user.UserName}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }

        // ===== PHASE 2: COURSES, TAGS =====

        private async Task SeedCoursesAsync()
        {
            if (await _context.Courses.AnyAsync()) return;

            var instructors = await _context.Users.OfType<Instructor>().ToListAsync();
            if (!instructors.Any()) { Console.WriteLine("No instructors found, skipping courses."); return; }

            var courseData = new List<(string Name, string Description, CourseStatus Status)>
            {
                ("Lập trình C# cơ bản đến nâng cao", "Khóa học dành cho người mới bắt đầu với C# và .NET, bao gồm OOP, LINQ và Async.", CourseStatus.Public),
                ("Thiết kế Web với React & Tailwind", "Xây dựng giao diện hiện đại với ReactJS và Tailwind CSS từ dự án thực tế.", CourseStatus.Public),
                ("Backend với ASP.NET Core Web API", "Xây dựng hệ thống backend hiệu năng cao, bảo mật với RESTful API.", CourseStatus.Public),
                ("Cơ sở dữ liệu PostgreSQL chuyên sâu", "Thiết kế, tối ưu và quản trị database PostgreSQL cho production.", CourseStatus.Public),
                ("Machine Learning với Python", "Nhập môn AI và Machine Learning thực tế với scikit-learn và TensorFlow.", CourseStatus.Public),
                ("DevOps cơ bản: Docker & CI/CD", "Tự động hóa quy trình triển khai phần mềm với Docker và GitHub Actions.", CourseStatus.Public),
                ("Thiết kế UI/UX Mobile App", "Quy trình thiết kế ứng dụng di động chuyên nghiệp với Figma.", CourseStatus.Public),
                ("Lập trình Java Spring Boot", "Xây dựng ứng dụng doanh nghiệp với Spring Boot và microservices.", CourseStatus.Private),
                ("Blockchain và Smart Contract", "Khám phá thế giới Web3 và lập trình Solidity trên Ethereum.", CourseStatus.Private),
                ("An toàn thông tin ứng dụng Web", "Bảo mật ứng dụng và phòng chống tấn công theo OWASP Top 10.", CourseStatus.Private)
            };

            for (int i = 0; i < courseData.Count; i++)
            {
                var data = courseData[i];
                var instructor = instructors[i % instructors.Count];
                var createdDate = DateTime.UtcNow.AddDays(-_random.Next(10, 60));

                _context.Courses.Add(new Course
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = data.Name,
                    Description = data.Description,
                    Price = (decimal)(_random.Next(10, 100) * 10000),
                    Status = data.Status,
                    ImageUrl = $"https://picsum.photos/seed/course{i}/400/300",
                    InstructorId = instructor.Id,
                    CreateTime = createdDate,
                    UpdatedAt = createdDate.AddDays(_random.Next(1, 5))
                });
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 10 courses.");
        }

        private async Task SeedTagsAsync()
        {
            if (await _context.Tags.AnyAsync()) return;

            var tags = new List<Tag>
            {
                new Tag { Id = Guid.NewGuid().ToString(), Name = ".NET", Description = "Microsoft .NET Ecosystem" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Frontend", Description = "Web Client Development" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Backend", Description = "Server Side Development" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "DevOps", Description = "Infrastructure and Automation" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "AI", Description = "Artificial Intelligence & ML" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Database", Description = "Data Storage and Management" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "UI/UX", Description = "Design and User Experience" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Security", Description = "Cybersecurity and Protection" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Web3", Description = "Blockchain & Decentralized" },
                new Tag { Id = Guid.NewGuid().ToString(), Name = "Mobile", Description = "Mobile App Development" }
            };

            await _context.Tags.AddRangeAsync(tags);
            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 10 tags.");
        }

        private async Task SeedCourseTagsAsync()
        {
            if (await _context.CourseTags.AnyAsync()) return;

            var courses = await _context.Courses.ToListAsync();
            var tags = await _context.Tags.ToListAsync();

            foreach (var course in courses)
            {
                var selectedTags = tags.OrderBy(_ => _random.Next()).Take(3).ToList();
                foreach (var tag in selectedTags)
                {
                    _context.CourseTags.Add(new CourseTag { CourseId = course.Id, TagId = tag.Id });
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Assigned 3 tags to each course.");
        }

        // ===== PHASE 3: COURSE CONTENT =====

        private async Task SeedCourseContentAsync()
        {
            if (await _context.Lectures.AnyAsync()) return;

            var courses = await _context.Courses.ToListAsync();
            foreach (var course in courses)
            {
                for (int l = 1; l <= 4; l++)
                {
                    var lecture = new Lecture
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Chương {l}: Nội dung bài học thứ {l}",
                        Description = $"Kiến thức trọng tâm của chương {l} trong khóa học.",
                        DisplayOrder = l,
                        CourseId = course.Id
                    };
                    _context.Lectures.Add(lecture);

                    // 2 Videos per lecture
                    for (int v = 1; v <= 2; v++)
                    {
                        _context.LectureVideos.Add(new LectureVideo
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"Video {l}.{v}: Bài giảng phần {v}",
                            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                            Duration = _random.Next(300, 1200),
                            DisplayOrder = v,
                            LectureId = lecture.Id
                        });
                    }

                    // 1 Document per lecture
                    _context.Documents.Add(new Document
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Tài liệu tham khảo chương {l}",
                        Url = "https://example.com/doc.pdf",
                        Type = "pdf",
                        LectureId = lecture.Id
                    });

                    // 1 Quiz per lecture
                    _context.Quizzes.Add(new Quiz
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Bài trắc nghiệm chương {l}",
                        TestTime = 15,
                        LectureId = lecture.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 40 lectures, 80 videos, 40 documents, 40 quizzes.");
        }

        private async Task SeedQuestionsAndOptionsAsync()
        {
            if (await _context.Questions.AnyAsync()) return;

            var quizzes = await _context.Quizzes.ToListAsync();

            var questionBank = new List<(string Content, string Explanation, string[] Options, int CorrectIndex)>
            {
                ("Đâu là kiểu dữ liệu tham chiếu trong C#?", "string, object, array đều là kiểu tham chiếu.", new[] { "string", "int", "bool", "char" }, 0),
                ("HTTP status 404 nghĩa là gì?", "404 = Not Found, tài nguyên không tìm thấy trên server.", new[] { "Not Found", "Internal Server Error", "Unauthorized", "Bad Request" }, 0),
                ("Lệnh nào dùng để cài đặt package trong npm?", "npm install hoặc npm i là lệnh cài package.", new[] { "npm install", "npm get", "npm download", "npm fetch" }, 0),
            };

            foreach (var quiz in quizzes)
            {
                for (int q = 0; q < questionBank.Count; q++)
                {
                    var qData = questionBank[q];
                    var question = new Question
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = qData.Content,
                        DisplayOrder = q + 1,
                        Explanation = qData.Explanation,
                        QuizId = quiz.Id
                    };
                    _context.Questions.Add(question);

                    for (int o = 0; o < qData.Options.Length; o++)
                    {
                        _context.QuestionOptions.Add(new QuestionOption
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = qData.Options[o],
                            IsCorrect = o == qData.CorrectIndex,
                            DisplayOrder = o + 1,
                            QuestionId = question.Id
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 120 questions and 480 options.");
        }

        // ===== PHASE 4: ENROLLMENT, PAYMENT, COURSE REQUESTS =====

        private async Task SeedEnrollmentsAndCommentsAsync()
        {
            if (await _context.Enrollments.AnyAsync()) return;

            var courses = await _context.Courses.ToListAsync();
            // Lấy tất cả user (Student + Instructor đều có thể enroll vì đều có role Student)
            // Nhưng Order.StudentId FK trỏ tới Student entity, nên chỉ query Student entities
            var students = await _context.Users.OfType<Student>().ToListAsync();

            if (!students.Any() || !courses.Any())
            {
                Console.WriteLine("No students or courses found, skipping enrollments.");
                return;
            }

            var enrolledPairs = new HashSet<(string StudentId, string CourseId)>();
            var commentTexts = new[]
            {
                "Khóa học rất hay và bổ ích, giảng viên nhiệt tình!",
                "Nội dung chất lượng, giải thích dễ hiểu.",
                "Mình rất thích cách trình bày của giảng viên.",
                "Bài tập thực hành phong phú, học được nhiều.",
                "Khóa học đáng giá, mình sẽ giới thiệu cho bạn bè.",
                "Có vài chỗ hơi nhanh nhưng tổng thể rất tốt.",
                "Giảng viên hỗ trợ câu hỏi rất nhanh chóng.",
                "Kiến thức áp dụng được ngay vào công việc thực tế."
            };

            foreach (var course in courses)
            {
                // Pick 3 unique students for each course
                var selectedStudents = students.OrderBy(_ => _random.Next()).Take(3).ToList();

                foreach (var student in selectedStudents)
                {
                    // Prevent duplicate enrollment
                    if (!enrolledPairs.Add((student.Id, course.Id)))
                        continue;

                    var orderDate = DateTime.UtcNow.AddDays(-_random.Next(5, 30));
                    var paidDate = orderDate.AddMinutes(_random.Next(5, 120));

                    var order = new Order
                    {
                        Id = Guid.NewGuid().ToString(),
                        StudentId = student.Id,
                        TotalAmount = course.Price,
                        CreatedAt = orderDate,
                        Status = "Paid",
                        PaymentMethod = "MoMo",
                        PaidAt = paidDate
                    };
                    _context.Orders.Add(order);

                    _context.OrderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderId = order.Id,
                        CourseId = course.Id,
                        Price = course.Price,
                        FinalPrice = course.Price
                    });

                    var enrollment = new Enrollment
                    {
                        Id = Guid.NewGuid().ToString(),
                        StudentId = student.Id,
                        CourseId = course.Id,
                        OrderId = order.Id,
                        EnrolledAt = paidDate,
                        ExpiresAt = paidDate.AddYears(1),
                        Status = true,
                        LastVisit = DateTime.UtcNow.AddDays(-_random.Next(0, 5))
                    };
                    _context.Enrollments.Add(enrollment);

                    // One review comment per enrollment
                    _context.Comments.Add(new Comment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = commentTexts[_random.Next(commentTexts.Length)],
                        Rate = _random.Next(3, 6),
                        CreatedAt = paidDate.AddDays(_random.Next(1, 10)),
                        Type = CommentType.Review,
                        EnrollmentId = enrollment.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"Seeded {enrolledPairs.Count} enrollments with orders and comments.");
        }

        private async Task SeedPaymentTransactionsAsync()
        {
            if (await _context.PaymentTransactions.AnyAsync()) return;

            var orders = await _context.Orders.Where(o => o.Status == "Paid").ToListAsync();
            foreach (var order in orders)
            {
                _context.PaymentTransactions.Add(new PaymentTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    GatewayTransactionId = "TXN" + RandomNumberString(10),
                    GatewayToken = "TKN" + RandomNumberString(12),
                    Amount = order.TotalAmount,
                    PaymentStatus = "Success",
                    TransactionDate = order.PaidAt ?? order.CreatedAt,
                    GatewayResponse = "SUCCESS",
                    ErrorCode = "00"
                });
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"Seeded {orders.Count} payment transactions.");
        }

        private async Task SeedCourseRequestsAsync()
        {
            if (await _context.CourseRequests.AnyAsync()) return;

            var courses = await _context.Courses.ToListAsync();
            foreach (var course in courses)
            {
                var isPublic = course.Status == CourseStatus.Public;
                _context.CourseRequests.Add(new CourseRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    CourseId = course.Id,
                    InstructorId = course.InstructorId,
                    Status = isPublic ? RequestStatus.Approved : RequestStatus.Pending,
                    CreatedAt = course.CreateTime.AddHours(1),
                    ProcessedAt = isPublic ? course.CreateTime.AddDays(1) : null
                });
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 10 course requests.");
        }

        // ===== PHASE 5: STUDENT ACTIVITY =====

        private async Task SeedQuizAttemptsAsync()
        {
            if (await _context.QuizAttempts.AnyAsync()) return;

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Lectures)
                    .ThenInclude(l => l.Quizzes)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                // Each student attempts the quiz of the first lecture
                var firstQuiz = enrollment.Course.Lectures
                    .OrderBy(l => l.DisplayOrder)
                    .FirstOrDefault()?.Quizzes.FirstOrDefault();

                if (firstQuiz == null) continue;

                var questions = await _context.Questions
                    .Where(q => q.QuizId == firstQuiz.Id)
                    .Include(q => q.QuestionOptions)
                    .ToListAsync();

                if (!questions.Any()) continue;

                // Randomly decide how many correct answers (for realistic scores)
                int correctCount = _random.Next(1, questions.Count + 1);
                var shuffledQuestions = questions.OrderBy(_ => _random.Next()).ToList();

                var attempt = new QuizAttempt
                {
                    Id = Guid.NewGuid().ToString(),
                    EnrollmentId = enrollment.Id,
                    QuizId = firstQuiz.Id,
                    AttemptedAt = enrollment.EnrolledAt.AddDays(_random.Next(1, 5)),
                    CompletedAt = enrollment.EnrolledAt.AddDays(_random.Next(1, 5)).AddMinutes(_random.Next(5, 15)),
                    Score = (int)Math.Round((double)correctCount / questions.Count * 100)
                };
                _context.QuizAttempts.Add(attempt);

                for (int i = 0; i < shuffledQuestions.Count; i++)
                {
                    var question = shuffledQuestions[i];
                    QuestionOption? selectedOption;

                    if (i < correctCount)
                    {
                        // Pick correct answer
                        selectedOption = question.QuestionOptions.FirstOrDefault(o => o.IsCorrect);
                    }
                    else
                    {
                        // Pick a wrong answer
                        selectedOption = question.QuestionOptions.FirstOrDefault(o => !o.IsCorrect);
                    }

                    _context.QuizAttemptAnswers.Add(new QuizAttemptAnswer
                    {
                        Id = Guid.NewGuid().ToString(),
                        QuizAttemptId = attempt.Id,
                        QuestionId = question.Id,
                        SelectedOptionId = selectedOption?.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded quiz attempts and answers.");
        }

        private async Task SeedStudentLectureProgressAsync()
        {
            if (await _context.StudentLectureProgresses.AnyAsync()) return;

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.LectureVideos.OrderBy(v => v.DisplayOrder))
                .Include(e => e.Course)
                    .ThenInclude(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.Documents)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.Quizzes)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                var lectures = enrollment.Course.Lectures.OrderBy(l => l.DisplayOrder).ToList();
                // Each student completes 1-2 lectures worth of content
                int lecturesToComplete = _random.Next(1, Math.Min(3, lectures.Count + 1));

                for (int i = 0; i < lecturesToComplete; i++)
                {
                    var lecture = lectures[i];

                    // Complete all videos in this lecture
                    foreach (var video in lecture.LectureVideos)
                    {
                        _context.StudentLectureProgresses.Add(new StudentLectureProgress
                        {
                            Id = Guid.NewGuid().ToString(),
                            StudentId = enrollment.StudentId,
                            CourseId = enrollment.CourseId,
                            LectureId = lecture.Id,
                            ItemId = video.Id,
                            ItemType = "Video",
                            IsCompleted = true
                        });
                    }

                    // Complete document
                    foreach (var doc in lecture.Documents)
                    {
                        _context.StudentLectureProgresses.Add(new StudentLectureProgress
                        {
                            Id = Guid.NewGuid().ToString(),
                            StudentId = enrollment.StudentId,
                            CourseId = enrollment.CourseId,
                            LectureId = lecture.Id,
                            ItemId = doc.Id,
                            ItemType = "Document",
                            IsCompleted = true
                        });
                    }

                    // Complete quiz (only for first lecture since we seeded attempts for first quiz)
                    foreach (var quiz in lecture.Quizzes)
                    {
                        _context.StudentLectureProgresses.Add(new StudentLectureProgress
                        {
                            Id = Guid.NewGuid().ToString(),
                            StudentId = enrollment.StudentId,
                            CourseId = enrollment.CourseId,
                            LectureId = lecture.Id,
                            ItemId = quiz.Id,
                            ItemType = "Quiz",
                            IsCompleted = true
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded student lecture progress.");
        }

        // ===== PHASE 6: GIFT CODES & NOTIFICATIONS =====

        private async Task SeedGiftCodesAsync()
        {
            if (await _context.GiftCodes.AnyAsync()) return;

            var courses = await _context.Courses.Take(3).ToListAsync();
            var students = await _context.Users.OfType<Student>().Take(3).ToListAsync();
            var admin = await _context.Users.OfType<Admin>().FirstOrDefaultAsync();
            var adminId = admin?.Id ?? Guid.NewGuid().ToString();

            // 5 Active gift codes (some linked to courses, some universal)
            for (int i = 0; i < 5; i++)
            {
                _context.GiftCodes.Add(new GiftCode
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = "GIFT" + (i + 1) + RandomNumberString(4),
                    CourseId = i < courses.Count ? courses[i].Id : null,
                    IsActive = true,
                    MaxUses = 10,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 10)),
                    ExpiryDate = DateTime.UtcNow.AddMonths(1)
                });
            }

            // 3 Used gift codes
            for (int i = 0; i < students.Count; i++)
            {
                _context.GiftCodes.Add(new GiftCode
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = "USED" + (i + 1) + RandomNumberString(4),
                    IsActive = true,
                    MaxUses = 1,
                    UsageCount = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(5, 15))
                });
            }

            // 2 Expired gift codes
            for (int i = 0; i < 2; i++)
            {
                _context.GiftCodes.Add(new GiftCode
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = "EXP" + (i + 1) + RandomNumberString(4),
                    IsActive = false,
                    MaxUses = 1,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2),
                    ExpiryDate = DateTime.UtcNow.AddMonths(-1)
                });
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Seeded 10 gift codes.");
        }

        private async Task SeedNotificationsAsync()
        {
            if (await _context.Notifications.AnyAsync()) return;

            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                // Welcome notification
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    Title = "Chào mừng bạn đến với VietEdu!",
                    Message = $"Xin chào {user.FullName}, chúc bạn có trải nghiệm học tập tuyệt vời trên hệ thống!",
                    Type = NotificationType.System,
                    IsRead = true,
                    CreatedAt = user.CreatedAt
                });

                // System update notification
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    Title = "Cập nhật hệ thống",
                    Message = "Chúng tôi vừa nâng cấp tính năng mới. Khám phá ngay!",
                    Type = NotificationType.System,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                });
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"Seeded {users.Count * 2} notifications.");
        }

        // ===== HELPER =====

        private string RandomNumberString(int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = (char)('0' + _random.Next(10));
            return new string(chars);
        }
    }
}