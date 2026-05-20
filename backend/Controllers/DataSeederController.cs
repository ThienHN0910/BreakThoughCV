using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/data-seeding")]
public class DataSeederController : ControllerBase
{
    private readonly MongoDbService _db;

    public DataSeederController(MongoDbService db)
    {
        _db = db;
    }

    [HttpPost("seed-test-data")]
    public async Task<IActionResult> SeedTestData()
    {
        try
        {
            // Initialize categories
            var categoryCount = await _db.Categories.CountDocumentsAsync(Builders<Category>.Filter.Empty);
            if (categoryCount == 0)
            {
                var defaultCategories = new[]
                {
                    new Category { Name = "Software Development", Slug = "software-development" },
                    new Category { Name = "Data Science", Slug = "data-science" },
                    new Category { Name = "DevOps & Infrastructure", Slug = "devops-infrastructure" },
                    new Category { Name = "Mobile Development", Slug = "mobile-development" },
                    new Category { Name = "Full Stack", Slug = "full-stack" },
                    new Category { Name = "UI/UX Design", Slug = "uiux-design" },
                    new Category { Name = "Business & Management", Slug = "business-management" },
                    new Category { Name = "Marketing", Slug = "marketing" }
                };
                await _db.Categories.InsertManyAsync(defaultCategories);
            }

            // Create test recruiter user
            var testRecruiter = new User
            {
                Email = "recruiter.test@example.com",
                Name = "Test Recruiter",
                Role = "recruiter",
                CreatedAt = DateTime.UtcNow
            };

            var filter = Builders<User>.Filter.Eq(u => u.Email, testRecruiter.Email);
            var existingRecruiter = await _db.Users.Find(filter).FirstOrDefaultAsync();
            if (existingRecruiter == null)
            {
                await _db.Users.InsertOneAsync(testRecruiter);
            }
            else
            {
                testRecruiter = existingRecruiter;
            }

            // Get categories for reference
            var categories = await _db.Categories.Find(Builders<Category>.Filter.Empty).ToListAsync();

            // Expanded companies list (8 companies)
            var companies = new[]
            {
                new Company
                {
                    Name = "TechVenture Solutions",
                    Description = "Leading software development company specializing in cloud solutions and microservices",
                    Website = "https://techventure.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "Digital Innovation Labs",
                    Description = "AI and machine learning solutions provider for enterprise",
                    Website = "https://diginnovate.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "E-Commerce Masters",
                    Description = "Largest e-commerce platform in Southeast Asia with millions of users",
                    Website = "https://ecomasters.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "Quantum Computing Inc",
                    Description = "Pioneering quantum computing solutions for financial and scientific industries",
                    Website = "https://quantumcomp.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "CloudNine Systems",
                    Description = "Cloud infrastructure and DevOps solutions for global enterprises",
                    Website = "https://cloudnine.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "DesignForward Studio",
                    Description = "Award-winning UI/UX design and product design agency",
                    Website = "https://designforward.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "DataWave Analytics",
                    Description = "Big data analytics and business intelligence platform",
                    Website = "https://datawave.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "MobileFirst Apps",
                    Description = "Mobile app development studio specializing in iOS and Android",
                    Website = "https://mobilefirst.example.com",
                    RecruiterId = testRecruiter.Id!
                }
            };

            var companyFilter = Builders<Company>.Filter.Empty;
            var existingCompanies = await _db.Companies.Find(companyFilter).ToListAsync();
            var companyIds = new List<string>();

            // If we don't have all companies, clear and reseed
            if (existingCompanies.Count < 8)
            {
                await _db.Companies.DeleteManyAsync(Builders<Company>.Filter.Empty);
                await _db.Companies.InsertManyAsync(companies);
                companyIds = companies.Select(c => c.Id!).ToList();
            }
            else
            {
                companyIds = existingCompanies.Select(c => c.Id!).ToList();
            }

            // Expanded jobs list (20+ job titles)
            var jobTitles = new[]
            {
                ("Senior Backend Engineer", "Build scalable microservices and APIs", 5, 0, new[] {"C#", "Node.js", ".NET"}),
                ("Frontend Developer (React)", "Build modern web interfaces with React and TypeScript", 3, 0, new[] {"React", "TypeScript", "CSS"}),
                ("Full Stack Developer", "Build complete feature sets from frontend to backend", 4, 4, new[] {"React", ".NET", "MongoDB"}),
                ("DevOps Engineer", "Manage cloud infrastructure and CI/CD pipelines", 5, 2, new[] {"Kubernetes", "Docker", "AWS"}),
                ("Data Science Engineer", "Build ML models for recommendations and predictions", 3, 1, new[] {"Python", "TensorFlow", "SQL"}),
                ("Mobile Developer iOS", "Develop native iOS applications with Swift", 2, 3, new[] {"Swift", "iOS", "REST APIs"}),
                ("Mobile Developer Android", "Develop native Android applications", 2, 3, new[] {"Kotlin", "Android", "Java"}),
                ("ML Engineer", "Advanced machine learning model development", 4, 1, new[] {"Python", "PyTorch", "AWS SageMaker"}),
                ("Cloud Architect", "Design and implement cloud infrastructure solutions", 7, 2, new[] {"AWS", "Azure", "Terraform"}),
                ("Security Engineer", "Implement security best practices and SIEM solutions", 5, 2, new[] {"Security", "Cryptography", "AWS"}),
                ("UI/UX Designer", "Design beautiful user experiences", 3, 5, new[] {"Figma", "Design Systems", "User Research"}),
                ("Product Manager", "Lead product strategy and development", 5, 6, new[] {"Product Strategy", "Analytics", "Agile"}),
                ("QA Engineer", "Quality assurance and test automation", 2, 0, new[] {"Selenium", "Testing", "Automation"}),
                ("Solutions Architect", "Architect solutions for enterprise clients", 8, 6, new[] {"Architecture", "Consulting", "Technical Sales"}),
                ("Platform Engineer", "Build internal development tools and platforms", 4, 2, new[] {"Go", "Kubernetes", "CI/CD"}),
                ("Database Administrator", "Manage and optimize database systems", 5, 1, new[] {"MongoDB", "PostgreSQL", "Performance Tuning"}),
                ("AI Research Scientist", "Research new AI techniques and algorithms", 6, 1, new[] {"Research", "Python", "Deep Learning"}),
                ("Growth Hacker", "Growth strategy and marketing automation", 3, 7, new[] {"Analytics", "Marketing", "SEO"}),
                ("API Designer", "Design RESTful and GraphQL APIs", 4, 4, new[] {"API Design", "REST", "GraphQL"}),
                ("Integration Engineer", "Integrate third-party systems and services", 3, 0, new[] {"Integration", "APIs", "Webhooks"})
            };

            var jobFilter = Builders<Job>.Filter.Empty;
            var existingJobs = await _db.Jobs.Find(jobFilter).ToListAsync();
            if (existingJobs.Count < 20)
            {
                // Clear existing jobs to reseed
                if (existingJobs.Count > 0)
                    await _db.Jobs.DeleteManyAsync(Builders<Job>.Filter.Empty);

                var jobs = new List<Job>();
                int jobIndex = 0;

                foreach (var companyId in companyIds)
                {
                    // Each company gets 3 jobs
                    for (int i = 0; i < 3; i++)
                    {
                        var (title, desc, minExp, categoryIndex, skills) = jobTitles[jobIndex % jobTitles.Length];
                        var category = categories.ElementAtOrDefault(categoryIndex);

                        var job = new Job
                        {
                            Title = title,
                            Description = desc,
                            CompanyId = companyId,
                            CategoryId = category?.Id,
                            MinExperienceYears = minExp,
                            MustHaveSkills = new List<string>(skills ?? new[] { "Technical Skills" }),
                            Responsibilities = new List<string>
                            {
                                "Develop and maintain features",
                                "Code review and mentoring",
                                "Collaborate with team members",
                                "Optimize performance"
                            }
                        };

                        jobs.Add(job);
                        jobIndex++;
                    }
                }

                await _db.Jobs.InsertManyAsync(jobs);
            }

            return Ok(new
            {
                message = "Test data seeded successfully",
                categories = await _db.Categories.CountDocumentsAsync(Builders<Category>.Filter.Empty),
                companies = await _db.Companies.CountDocumentsAsync(Builders<Company>.Filter.Empty),
                jobs = await _db.Jobs.CountDocumentsAsync(Builders<Job>.Filter.Empty),
                users = await _db.Users.CountDocumentsAsync(Builders<User>.Filter.Empty)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
