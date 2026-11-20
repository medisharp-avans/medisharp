using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Server.Filters
{
    /// <summary>Temporary boundary until PRI-03 supplies authentication and referral access rules.</summary>
    public class DevelopmentOnlyFilter(IHostEnvironment environment) : IAsyncResourceFilter
    {
        /// <summary>Rejects requests outside Development before model binding or persistence runs.</summary>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (!environment.IsDevelopment())
            {
                context.Result = new NotFoundResult();
                return;
            }

            await next();
        }
    }
}
