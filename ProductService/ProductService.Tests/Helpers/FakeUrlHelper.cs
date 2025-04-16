using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace ProductService.Tests.Helpers
{
    public class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new ActionContext();

        public string Action(UrlActionContext actionContext) => "http://fakeurl";

        public string Content(string contentPath) => contentPath;

        public bool IsLocalUrl(string url) => true;

        public string Link(string? routeName, object? values) => "http://fakeurl";

        public string RouteUrl(UrlRouteContext routeContext) => "http://fakeroute";
    }
}
