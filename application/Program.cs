using Microsoft.AspNetCore.Mvc;
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

var blogEndpoints = app.MapGroup("blogs");
blogEndpoints.MapGet("/", (SomeDbContext db) => db.Blogs.ToListAsync());

blogEndpoints.MapPost("/", async ([FromBody] BlogRequest blog, SomeDbContext db) =>
{
    var entity = new Blog { Title = blog.Title };
    await db.AddAsync(entity);
    await db.SaveChangesAsync();
    return Results.Created($"/blogs/{entity.BlogId}", new BlogResponse(entity.BlogId, entity.Title));
});

blogEndpoints.MapGet("/{id}", async (int id, SomeDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation($"got request for id {id}");
    Blog? blog = await db.Blogs.Where(b => b.BlogId == id).FirstOrDefaultAsync();
    return blog is null ? Results.NotFound() : Results.Ok(new BlogResponse(blog.BlogId, blog.Title));
});

blogEndpoints.MapDelete("/{id}", async (int id, SomeDbContext db) =>
{
    await db.Blogs.Where(b => b.BlogId == id).ExecuteDeleteAsync();
    return Results.Ok();
});

app.Run();

public record BlogRequest(string Title);
public record BlogResponse(int Id, string Title);

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

