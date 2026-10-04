using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Zamfara.Web.Infrastructure;

namespace Zamfara.Web.TagHelpers;

// Runs after MVC's AnchorTagHelper has generated the local href.
[HtmlTargetElement("a", Attributes = "asp-action")]
public sealed class SchoolNavigationTagHelper(IWebHostEnvironment environment) : TagHelper
{
    public override int Order => 0;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var request = ViewContext.HttpContext.Request;
        var school = ViewContext.HttpContext.GetSchool();
        if (!environment.IsDevelopment() || school is null ||
            string.IsNullOrEmpty(request.Query["school"]))
            return;

        if (output.Attributes.TryGetAttribute("href", out var href))
            output.Attributes.SetAttribute("href",
                QueryHelpers.AddQueryString(href.Value.ToString()!, "school", school.Slug));
    }
}
