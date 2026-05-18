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
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        await _db.Categories.InsertOneAsync(category);
        return CreatedAtAction(nameof(GetAll), new { id = category.Id }, category);
    }
}
