using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using TestOurUniverse.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//추가한 서비스
//builder.Services.AddDbContext<ApppDbContext>
//    (options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddAuthentication();
//builder.Services.AddIdentityApiEndpoints<IdentityUser>().AddEntityFrameworkStores<ApppDbContext>();

//builder.Services.AddSwaggerGen(options =>
//{
//    options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//    {
//        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//        Name = "Authorization",
//        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
//    });
//    options.OperationFilter<SecurityRequirementsOperationFilter>();
//});


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
