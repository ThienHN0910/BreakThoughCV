using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly MongoDbService _db;

    public CategoryController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories.Find(_ => true).ToListAsync();
        return Ok(categories);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var category = new Category { Name = request.Name, Slug = request.Slug };
        await _db.Categories.InsertOneAsync(category);
        return CreatedAtAction(nameof(GetAll), new { id = category.Id }, category);
    }

    [HttpPost("init-defaults")]
    public async Task<IActionResult> InitializeDefaults()
    {
        var existingCount = await _db.Categories.CountDocumentsAsync(_ => true);
        if (existingCount > 0)
            return Ok(new { message = "Categories already exist" });

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
        return Ok(new { message = "Default categories initialized", count = defaultCategories.Length });
    }
}
