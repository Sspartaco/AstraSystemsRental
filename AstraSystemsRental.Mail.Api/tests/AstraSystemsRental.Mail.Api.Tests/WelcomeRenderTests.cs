using AstraSystemsRental.Mail.Api.Dtos;
using AstraSystemsRental.Mail.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AstraSystemsRental.Mail.Api.Tests;

public class WelcomeRenderTests
{
    [Fact(DisplayName = "Razor Welcome template renders the model into HTML5")]
    public async Task Welcome_Template_RendersModel()
    {
        // Arrange
        var contentRoot = Path.Combine(AppContext.BaseDirectory);
        var environment = new StubWebHostEnvironment { ContentRootPath = contentRoot };
        var renderer = new RazorTemplateRenderer(environment);
        var model = new WelcomeModel("Jonathan", "https://astra.local/confirm?token=xyz", 2026);

        // Act
        var html = await renderer.RenderAsync("Welcome.cshtml", model);

        // Assert
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("Jonathan");
        html.Should().Contain("https://astra.local/confirm?token=xyz");
        html.Should().Contain("2026");
    }

    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "AstraSystemsRental.Mail.Api";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
