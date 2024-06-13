using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace application;

public static class BlogEndpoints
{
    public record CreateRequest(string Title);
    public record BlogEntity(int Id, string Title);

    public static IEndpointRouteBuilder MapBlogs(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("blogs");

        group.MapGet("/", GetBlogs);
        group.MapPost("/", CreateBlog);

        group.MapGet("/{id}", GetBlog);
        group.MapPut("/{id}", UpdateBlog);
        group.MapDelete("/{id}", DeleteBlog);

        return group;
    }

    private static async Task<IResult> GetBlogs(SomeDbContext db, int pageSize = 10, int? after = null)
    {
        if (pageSize < 1) return TypedResults.BadRequest($"pageSize should be a positive number, got {pageSize}");
        pageSize = Math.Min(pageSize, 100);

        IQueryable<Blog> query = db.Blogs;

        if (after is not null) query = query.Where(b => b.BlogId > after);

        List<Blog> entities = await query
            .OrderBy(b => b.BlogId)
            .Take(pageSize)
            .ToListAsync();

        var dtos = entities.Select(b => new BlogEntity(b.BlogId, b.Title)).ToList();
        return TypedResults.Ok(new Page<BlogEntity>(dtos, $"/blogs?pageSize={pageSize}&after={entities[^1].BlogId}"));
    }

    private static async Task<IResult> CreateBlog([FromBody] CreateRequest blog, SomeDbContext db)
    {
        var entity = new Blog { Title = blog.Title };
        await db.AddAsync(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/blogs/{entity.BlogId}", new BlogEntity(entity.BlogId, entity.Title));
    }

    private static async Task<IResult> GetBlog(int id, SomeDbContext db, ILogger<Program> logger)
    {
        logger.LogInformation($"got request for id {id}");
        Blog? blog = await db.Blogs.Where(b => b.BlogId == id).FirstOrDefaultAsync();
        return blog is null ? Results.NotFound() : Results.Ok(new BlogEntity(blog.BlogId, blog.Title));
    }

    private static async Task<IResult> UpdateBlog(int id, [FromBody] BlogEntity blog, SomeDbContext db)
    {
        if (blog.Id != id) return TypedResults.BadRequest("id from path and id from body must match");

        Blog? entity = await db.Blogs
            .Where(b => b.BlogId == id)
            .FirstOrDefaultAsync();

        if (entity is null) return TypedResults.NotFound();

        entity.Title = blog.Title;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new BlogEntity(entity.BlogId, entity.Title));
    }

    private static async Task<IResult> DeleteBlog(int id, SomeDbContext db)
    {
        await db.Blogs.Where(b => b.BlogId == id).ExecuteDeleteAsync();
        return Results.Ok();
    }
}
