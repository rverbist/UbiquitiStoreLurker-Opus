using Microsoft.EntityFrameworkCore;
using UbiquitiStoreLurker.Web.Data;
using UbiquitiStoreLurker.Web.Data.Entities;

namespace UbiquitiStoreLurker.Web.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", GetAllProducts)
            .WithName("GetProducts")
            .Produces<IReadOnlyList<ProductDto>>();

        group.MapGet("/{id:int}", GetProduct)
            .WithName("GetProduct")
            .Produces<ProductDto>()
            .ProducesProblem(404);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .Accepts<CreateProductRequest>("application/json")
            .Produces<ProductDto>(201)
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", UpdateProduct)
            .WithName("UpdateProduct")
            .Accepts<UpdateProductRequest>("application/json")
            .Produces<ProductDto>()
            .ProducesProblem(404);

        group.MapDelete("/{id:int}", DeleteProduct)
            .WithName("DeleteProduct")
            .Produces(204)
            .ProducesProblem(404);

        group.MapGet("/{id:int}/history", GetProductHistory)
            .WithName("GetProductHistory")
            .Produces<PagedResult<StockCheckDto>>()
            .ProducesProblem(404);

        return group;
    }

    private static async Task<IResult> GetAllProducts(UbiquitiStoreLurkerDbContext db, CancellationToken ct)
    {
        var products = await db.Products
            .OrderBy(p => p.Name ?? p.Url)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

        return Results.Ok(products);
    }

    private static async Task<IResult> GetProduct(int id, UbiquitiStoreLurkerDbContext db, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        return product is null
            ? Results.Problem(title: "Product not found", statusCode: 404)
            : Results.Ok(ToDto(product));
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        UbiquitiStoreLurkerDbContext db,
        CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(request.Url), ["Must be a valid absolute URL"] },
            });

        if (await db.Products.AnyAsync(p => p.Url == request.Url, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(request.Url), ["A product with this URL already exists"] },
            });

        var product = new Product
        {
            Url = request.Url,
            SubscribedEvents = request.SubscribedEvents,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/products/{product.Id}", ToDto(product));
    }

    private static async Task<IResult> UpdateProduct(
        int id,
        UpdateProductRequest request,
        UbiquitiStoreLurkerDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return Results.Problem(title: "Product not found", statusCode: 404);

        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;
        if (request.SubscribedEvents.HasValue) product.SubscribedEvents = request.SubscribedEvents.Value;

        product.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToDto(product));
    }

    private static async Task<IResult> DeleteProduct(int id, UbiquitiStoreLurkerDbContext db, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return Results.Problem(title: "Product not found", statusCode: 404);

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> GetProductHistory(
        int id,
        UbiquitiStoreLurkerDbContext db,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct))
            return Results.Problem(title: "Product not found", statusCode: 404);

        var total = await db.StockChecks.CountAsync(c => c.ProductId == id, ct);
        var items = await db.StockChecks
            .Where(c => c.ProductId == id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new StockCheckDto(
                c.Id, c.RequestUrl, c.HttpStatusCode, c.DetectedState,
                c.ParserStrategy, c.ParserConfidence, c.DurationMs,
                c.ErrorMessage, c.CreatedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(new PagedResult<StockCheckDto>(items, total, page, pageSize));
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Url, p.ProductCode, p.Name, p.Description,
        p.LocalImagePath ?? p.ImageUrl,
        p.CurrentState, p.IsActive, p.SubscribedEvents,
        p.NextPollDueAtUtc, p.LastPollAtUtc, p.LastStateChangeAtUtc,
        p.PollCount, p.ErrorCount);
}
