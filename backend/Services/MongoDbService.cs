using BreakThroughCV.API.Models;
using BreakThroughCV.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BreakThroughCV.API.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Category> Categories => _database.GetCollection<Category>("categories");
    public IMongoCollection<Company> Companies => _database.GetCollection<Company>("companies");
    public IMongoCollection<Job> Jobs => _database.GetCollection<Job>("jobs");
    public IMongoCollection<Application> Applications => _database.GetCollection<Application>("applications");
    public IMongoCollection<CvReview> CvReviews => _database.GetCollection<CvReview>("cvReviews");
}
