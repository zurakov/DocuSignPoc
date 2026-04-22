using DocuSignPoc.Brokers.DocuSign;
using DocuSignPoc.Brokers.Storages;
using DocuSignPoc.Services.Foundations.DocuSign;
using DocuSignPoc.Services.Foundations.QuickBooks;
using DocuSignPoc.Services.Foundations.SignatureRequests;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SQLite Database Configuration
builder.Services.AddDbContext<SignatureRequestDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Brokers
builder.Services.AddTransient<IDocuSignBroker, DocuSignBroker>();
builder.Services.AddScoped<IStorageBroker, StorageBroker>();

// Foundation Services
builder.Services.AddTransient<IDocuSignService, DocuSignService>();
builder.Services.AddTransient<ISignatureRequestService, SignatureRequestService>();
builder.Services.AddTransient<IQuickBooksService, QuickBooksService>();

// Enable CORS if needed (though UI is now local)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto-Apply Migrations at Startup (Simplified for POC)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SignatureRequestDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
