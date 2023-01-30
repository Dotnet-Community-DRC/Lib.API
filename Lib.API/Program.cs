using FluentValidation;
using FluentValidation.Results;
using Lib.API.Data;
using Lib.API.Models;
using Lib.API.Services;
using Lib.API.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDbConnectionFactory>(
    _ => new SqliteConnectionFactory(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IBookService, BookService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("books", async (Book book, IBookService bookService,
            IValidator<Book> validator, LinkGenerator linker, HttpContext context) =>
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
}).WithName("CreateBook");

app.MapGet("books", async (IBookService bookService, string? searchTerm) =>
{
    if (searchTerm is not null && !string.IsNullOrWhiteSpace(searchTerm))
    {
        var matchedBooks = await bookService.SearchTermAsync(searchTerm);
        return Results.Ok(matchedBooks);
    }
    
    var books = await bookService.GetAllAsync();
    return Results.Ok(books);
}).WithName("GetBooks");

app.MapGet("books/{isbn}", async (string isbn, IBookService bookService) =>
{
    var book = await bookService.GetByIsbnAsync(isbn);
    return book is not null ? Results.Ok(book) : Results.NotFound();
}).WithName("GetBook");

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

}).WithName("UpdateBook");

app.MapDelete("books/{isbn}", async (string isbn, IBookService bookService)  =>
{
    var bookDeleted = await bookService.DeleteAsync(isbn);
    return bookDeleted ? Results.NoContent() : Results.NotFound();
}).WithName("DeleteBook");

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();