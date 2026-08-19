namespace Zamfara.Web.Infrastructure;

/// <summary>HttpContext.Items keys set by the tenant middleware.</summary>
public static class TenantKeys
{
    public const string School = "Tenant.School";
}

public static class TenantContextExtensions
{
    /// <summary>The school this request belongs to.</summary>
    public static Zamfara.Web.Models.School? GetSchool(this HttpContext context) =>
        context.Items[TenantKeys.School] as Zamfara.Web.Models.School;
}
