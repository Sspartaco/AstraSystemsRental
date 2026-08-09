using RazorLight;

namespace AstraSystemsRental.Mail.Api.Services;

public sealed class RazorTemplateRenderer : ITemplateRenderer
{
    private readonly RazorLightEngine _engine;

    public RazorTemplateRenderer(IWebHostEnvironment environment)
    {
        var root = Path.Combine(environment.ContentRootPath, "Views", "Emails");
        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(root)
            .UseMemoryCachingProvider()
            .Build();
    }

    public Task<string> RenderAsync<TModel>(string templateKey, TModel model) =>
        _engine.CompileRenderAsync(templateKey, model);
}
