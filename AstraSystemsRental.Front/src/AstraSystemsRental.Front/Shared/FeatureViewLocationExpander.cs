using Microsoft.AspNetCore.Mvc.Razor;

namespace AstraSystemsRental.Front.Shared;

public sealed class FeatureViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        context.Values["action"] = context.ActionContext.RouteData.Values["action"]?.ToString();
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var featureLocations = new[]
        {
            "/Features/{1}/Views/{0}.cshtml",
            "/Shared/Views/Shared/{0}.cshtml"
        };

        return featureLocations.Concat(viewLocations);
    }
}
