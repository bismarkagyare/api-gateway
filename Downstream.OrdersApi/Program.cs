var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet(
    "/orders",
    () =>
    {
        return Results.Ok(
            new[]
            {
                new { Id = 101, Product = "Keyboard", Quantity = 2, Status = "shipped" },
                new { Id = 102, Product = "Mouse", Quantity = 5, Status = "pending" },
            }
        );
    }
);

app.Run();
