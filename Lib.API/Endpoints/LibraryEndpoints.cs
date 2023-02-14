using FluentValidation;
using FluentValidation.Results;
using Lib.API.Endpoints.Internal;
using Lib.API.Models;
using Lib.API.Services;

namespace Lib.API.Endpoints;

public class LibraryEndpoints: IEndpoints
{
    public static void DefineEndpoints(IEndpointRouteBuilder app) 
    {
        app.MapPost("books", CreateBookAsync)
            .WithName("CreateBook")
            .Accepts<Book>("application/json")
            .Produces<Book>(201)
            .Produces<IEnumerable<ValidationFailure>>(400)
            .WithTags("Books");

        app.MapGet("books", async (IBookService bookService, string? searchTerm) =>
        {
            if (searchTerm is not null && !string.IsNullOrWhiteSpace(searchTerm))
            {
                var matchedBooks = await bookService.SearchTermAsync(searchTerm);
                return Results.Ok(matchedBooks);
            }
            
            var books = await bookService.GetAllAsync();
            return Results.Ok(books);
        })
            .WithName("GetBooks")
            .Produces<IEnumerable<Book>>(200)
            .WithTags("Books");

        app.MapGet("books/{isbn}", async (string isbn, IBookService bookService) =>
        {
            var book = await bookService.GetByIsbnAsync(isbn);
            return book is not null ? Results.Ok(book) : Results.NotFound();
        })
            .WithName("GetBook")
            .Produces<Book>(200)
            .Produces(401)
            .WithTags("Books");

        app.MapPut("books/{isbn}", async (string isbn,Book book, IBookService bookService,
            IValidator<Book> validator) =>
        {
            book.Isbn = isbn;
            
            var validationResult = await validator.ValidateAsync(book);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var updatedBook = await bookService.UpdateAsync(book);
            return updatedBook ? Results.Ok(book) : Results.NotFound();

        }).WithName("UpdateBook")
            .Accepts<Book>("application/json")
            .Produces<Book>(200)
            .Produces<IEnumerable<ValidationFailure>>(400) 
            .WithTags("Books");

        app.MapDelete("books/{isbn}", async (string isbn, IBookService bookService) =>
            {
                var bookDeleted = await bookService.DeleteAsync(isbn);
                return bookDeleted ? Results.NoContent() : Results.NotFound();
            }).WithName("DeleteBook")
            .Produces(204)
            .Produces(404)
            .WithTags("Books");
    }
    
    internal static async Task<IResult> CreateBookAsync(
        Book book, IBookService bookService,
        IValidator<Book> validator, LinkGenerator linker, HttpContext context
    )
    {

        var validationResult = await validator.ValidateAsync(book);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }
            
        var created = await bookService.CreateAsync(book);
        if (!created)
        {
            return Results.BadRequest(new List<ValidationFailure>()
            {
                new ("Isbn", "A book with this ISBN-13 already exists")
            });
        }

        var path = linker.GetPathByName("GetBook", new { isbn = book.Isbn })!;
        var locationUri = linker.GetUriByName(context, "GetBook", new { isbn = book.Isbn })!;
        return Results.Created(locationUri, book);

        // return Results.CreatedAtRoute("GetBook", new { isbn = book.Isbn }, book);
        // return Results.Created($"/books/{book.Isbn}", book);
           
    }
    
    public static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IBookService, BookService>();
    }
}