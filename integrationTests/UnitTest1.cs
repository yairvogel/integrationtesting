namespace integrationTests;

using System.Collections;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using static System.Net.HttpStatusCode;

public class UnitTest1(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ShouldCreateAPost()
    {
        using var client = factory.CreateClient();
        
        Guid guid = Guid.NewGuid();

        var blog = await CreateBlogAsync(client, guid);

        using var response2 = await client.GetAsync($"/blogs/{blog.Id}");
        response2.StatusCode.Should().Be(OK);
        (await response2.Content.ReadFromJsonAsync<BlogResponse>())!.Should().Be(blog);
        await DeleteBlogAsync(blog, client);
    }

    [Fact]
    public async Task ShouldPaginateCorrectly()
    {
        using var client = factory.CreateClient();
        List<BlogResponse> blogs = [];

        foreach (int i in 0..5)
        {
            blogs.Add(await CreateBlogAsync(client, Guid.NewGuid()));
        }

        using var response1 = await client.GetAsync("/blogs?pageSize=1");
        response1.StatusCode.Should().Be(OK);
        var page1 = await response1.Content.ReadFromJsonAsync<Page<BlogResponse>>();
        
        using var response2 = await client.GetAsync(page1!.next);
        response2.StatusCode.Should().Be(OK);
        var page2 = await response2.Content.ReadFromJsonAsync<Page<BlogResponse>>();

        page1.data.Count.Should().Be(1);
        page2!.data.Count.Should().Be(1);

        page2.data[0].Id.Should().BeGreaterThan(page1.data[0].Id);

        await Task.WhenAll(blogs.Select(b => DeleteBlogAsync(b, client)));
    }

    [Fact]
    public async Task ShouldUpdateABlog()
    {
        using var client = factory.CreateClient();
        var guid = Guid.NewGuid();
        var blog = await CreateBlogAsync(client, guid);

        var newGuid = Guid.NewGuid();

        using var put = await client.PutAsJsonAsync($"/blogs/{blog.Id}", blog with { Title = newGuid.ToString() });
        put.StatusCode.Should().Be(OK);
        var putBlog = await put.Content.ReadFromJsonAsync<BlogResponse>();

        putBlog!.Title.Should().Be(newGuid.ToString());
        putBlog.Id.Should().Be(blog.Id);

        using var get = await client.GetAsync($"/blogs/{blog.Id}");
        get.StatusCode.Should().Be(OK);
        var getBlog = await get.Content.ReadFromJsonAsync<BlogResponse>();

        getBlog.Should().Be(putBlog);

        await DeleteBlogAsync(blog, client);
    }

    private static async Task<BlogResponse> CreateBlogAsync(HttpClient client, Guid guid)
    {
        var title = guid.ToString();
        using var res = await client.PostAsync("/blogs", JsonContent.Create(new { Title = title }));
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        BlogResponse? blog = await res.Content.ReadFromJsonAsync<BlogResponse>();

        blog!.Title.Should().Be(title);
        blog.Id.Should().BePositive();
        return blog;
    }

    private static async Task DeleteBlogAsync(BlogResponse blog, HttpClient client)
    {
        using var res = await client!.DeleteAsync($"/blogs/{blog.Id}");
        res.StatusCode.Should().Be(OK);
    }


    private record BlogResponse(int Id, string Title);

    private record Page<T>(IReadOnlyList<T> data, string next);
}

static class RangeExtensions
{
    public static IEnumerator<int> GetEnumerator(this Range range) => new RangeEnumerator(range);

    private class RangeEnumerator(Range range) : IEnumerator<int>
    {
        public int Current { get; private set; } = range.Start.Value;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            return ++Current < range.End.Value;
        }

        public void Reset()
        {
            Current = range.Start.Value;
        }
    }
}
