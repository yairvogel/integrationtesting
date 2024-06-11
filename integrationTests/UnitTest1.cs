namespace integrationTests;

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

public class UnitTest1(WebApplicationFactory<Program> factory, ITestOutputHelper output) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HelloWorld()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldCreateAPost()
    {
        using var client = factory.CreateClient();
        
        string guid = Guid.NewGuid().ToString();

        int? id = null;
        using (var response1 = await client.PostAsync("/blogs", JsonContent.Create(new { Title = guid })))
        {
            response1.StatusCode.Should().Be(HttpStatusCode.Created);
            BlogResponse? blog = await response1.Content.ReadFromJsonAsync<BlogResponse>();
            blog!.Title.Should().Be(guid);
            id = blog.Id;
        }

        output.WriteLine($"got id {id}");

        using var response2 = await client.GetAsync($"/blogs/{id}");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response2.Content.ReadFromJsonAsync<BlogResponse>())!.Title.Should().Be(guid);

        using var response3 = await client.DeleteAsync($"/blogs/{id}");
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record BlogResponse(int Id, string Title);
}
