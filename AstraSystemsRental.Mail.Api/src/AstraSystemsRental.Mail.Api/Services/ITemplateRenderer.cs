namespace AstraSystemsRental.Mail.Api.Services;

public interface ITemplateRenderer
{
    Task<string> RenderAsync<TModel>(string templateKey, TModel model);
}
