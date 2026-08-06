using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FinanceSentry.API.Conventions;

/// <summary>
/// Prepends a global route prefix to every controller so individual controllers
/// do not need to repeat it. Register once in AddControllers().
/// </summary>
public sealed class ApiVersionPrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is not null
                    ? AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel)
                    : _prefix;
            }
        }
    }
}
