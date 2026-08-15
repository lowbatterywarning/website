namespace Zamfara.Web.Infrastructure;

/// <summary>HttpContext.Items keys set by the tenant middleware.</summary>
public static class TenantKeys
{
    public const string School = "Tenant.School";
    public const string IsPortal = "Tenant.IsPortal";
}

public static class TenantContextExtensions
{
    /// <summary>The school this request belongs to, or null for portal requests.</summary>
    public static Zamfara.Web.Models.School? GetSchool(this HttpContext context) =>
        context.Items[TenantKeys.School] as Zamfara.Web.Models.School;

    /// <summary>True when this request should render the portal directory.</summary>
    public static bool IsPortal(this HttpContext context) =>
        context.Items.ContainsKey(TenantKeys.IsPortal);
}
