using Foodtrucks.Api.Routing;

namespace Foodtrucks.Api.Features.Health
{
    public class SystemCheckFeature
    {
        public class CheckHealthCommand
        {
        }

        public class CheckHealthResponse
        {
            public string Status { get; set; } = "Healthy";
            public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        }

        public class CheckHealthCommandHandler
        {
            //private readonly IMenuService _menuService;
            //public CheckHealthCommandHandler(IMenuService menuService)
            //{
            //    _menuService = menuService;
            //}

            public CheckHealthCommandHandler()
            {

            }

            public async Task<CheckHealthResponse> Handle(CheckHealthCommand command)
            {
                // TODO make database call or other checks to verify system health
                // For now, we'll just return a healthy status
                return await Task.FromResult(new CheckHealthResponse());
            }
        }
        public class SystemCheckEndpoint : IEndpoint
        {
           

            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/health", async Task<IResult> () =>
                {
                    try
                    {
                        var command = new CheckHealthCommand();
                        //var handler = new CheckHealthCommandHandler(menuService);
                        var handler = new CheckHealthCommandHandler();
                        var result = await handler.Handle(command);
                        return Results.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                    }

                })
                .WithName("SystemCheck")
                .WithDescription("Checks the health of the api")
                .Produces<CheckHealthResponse>(StatusCodes.Status200OK)
                .WithOpenApi();
            }
        }
    }
}
