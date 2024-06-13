using application;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SomeDbContext>(options =>
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var dbPath = Path.Join(localAppData, "db.db");
    options.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddDbContext<SomeDbContext>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapBlogs();

app.Run();


public class SomeDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Blog> Blogs { get; set; } = null!;
}

public class Blog
{
    public int BlogId { get; set; }

    public string Title { get; set; } = null!;
}

public partial class Program {} // for WebApplicationFactory

