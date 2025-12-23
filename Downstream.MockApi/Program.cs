var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet(
    "/products",
    () =>
    {
        return Results.Ok(
            new[]
            {
                new
                {
                    Id = 1,
                    Name = "Keyboard",
                    Price = 100,
                },
                new
                {
                    Id = 2,
                    Name = "Mouse",
                    Price = 50,
                },
            }
        );
    }
);

app.Run();
