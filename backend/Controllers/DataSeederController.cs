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

            // Create test companies
            var companies = new[]
            {
                new Company
                {
                    Name = "TechVenture Solutions",
                    Description = "Leading software development company specializing in cloud solutions",
                    Website = "https://techventure.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "Digital Innovation Labs",
                    Description = "AI and machine learning solutions provider",
                    Website = "https://diginnovate.example.com",
                    RecruiterId = testRecruiter.Id!
                },
                new Company
                {
                    Name = "E-Commerce Masters",
                    Description = "Largest e-commerce platform in Southeast Asia",
                    Website = "https://ecomasters.example.com",
                    RecruiterId = testRecruiter.Id!
                }
            };

            var companyFilter = Builders<Company>.Filter.Empty;
            var existingCompanies = await _db.Companies.Find(companyFilter).ToListAsync();
            var companyIds = new List<string>();

            if (existingCompanies.Count == 0)
            {
                await _db.Companies.InsertManyAsync(companies);
                companyIds = companies.Select(c => c.Id!).ToList();
            }
            else
            {
                companyIds = existingCompanies.Select(c => c.Id!).ToList();
            }

            // Create test jobs
            var jobTitles = new[]
            {
                ("Senior Backend Engineer", "We're looking for an experienced backend engineer to build scalable APIs", 5, 0),
                ("Frontend Developer (React)", "Join our team to build modern web interfaces", 3, 0),
                ("DevOps Engineer", "Manage and optimize our cloud infrastructure", 5, 1),
                ("Data Science Engineer", "Build ML models for product recommendations", 3, 1),
                ("Full Stack Developer", "Build end-to-end features for our platform", 4, 2),
                ("Mobile App Developer (Flutter)", "Develop cross-platform mobile apps", 2, 3)
            };

            var jobFilter = Builders<Job>.Filter.Empty;
            var existingJobs = await _db.Jobs.Find(jobFilter).ToListAsync();
            if (existingJobs.Count == 0)
            {
                var jobs = new List<Job>();
                int jobIndex = 0;

                foreach (var companyId in companyIds)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var (title, desc, minExp, categoryIndex) = jobTitles[jobIndex % jobTitles.Length];
                        var category = categories.ElementAtOrDefault(categoryIndex);

                        var job = new Job
                        {
                            Title = title,
                            Description = desc,
                            CompanyId = companyId,
                            CategoryId = category?.Id,
                            MinExperienceYears = minExp,
                            MustHaveSkills = new List<string> { "Problem solving", "Communication" },
                            Responsibilities = new List<string> { "Build features", "Code review" }
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
