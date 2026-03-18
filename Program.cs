using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VoiceAssistantForBlind.Data;
using VoiceAssistantForBlind.Services;
using VoiceAssistantForBlind.Models;
using NerApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("user_type", "admin"));
    
    options.AddPolicy("UserOnly", policy =>
        policy.RequireClaim("user_type", "user"));
    
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// Register DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<ResumePdfService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Register Interview Module Services
builder.Services.AddScoped<ILLMService, GeminiLLMService>();
builder.Services.AddScoped<InterviewService>();

// Whisper STT singleton
builder.Services.AddSingleton<IWhisperTranscriptionService>(sp =>
{
    var cfg = builder.Configuration.GetSection("Whisper");
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    string modelPath = cfg["ModelPath"] ?? throw new InvalidOperationException("Whisper:ModelPath missing.");
    if (!Path.IsPathRooted(modelPath))
        modelPath = Path.Combine(env.ContentRootPath, modelPath);

    string language = cfg["Language"] ?? "en";
    return new WhisperTranscriptionService(modelPath, language);
});

// NER singleton
builder.Services.AddSingleton<INerService, NerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    Console.WriteLine($"✅ Database at: {Path.GetFullPath("VoiceAssistant.db")}");
    
    // Seed interview questions if none exist
    if (!dbContext.InterviewQuestions.Any())
    {
        Console.WriteLine("📚 Seeding interview questions...");
        SeedInterviewQuestions(dbContext);
        Console.WriteLine("✅ Interview questions seeded successfully!");
    }
}

app.Run();

// Helper method to seed interview questions
static void SeedInterviewQuestions(AppDbContext context)
{
    var questions = new List<InterviewQuestion>
    {
        // C# Questions
        new InterviewQuestion { 
            Topic = "C#", 
            Question = "Explain what object-oriented programming means in C#.", 
            Difficulty = "Beginner", 
            DisplayOrder = 1 
        },
        new InterviewQuestion { 
            Topic = "C#", 
            Question = "What are the differences between abstract classes and interfaces?", 
            Difficulty = "Intermediate", 
            DisplayOrder = 2 
        },
        new InterviewQuestion { 
            Topic = "C#", 
            Question = "Explain the difference between 'out' and 'ref' parameters.", 
            Difficulty = "Intermediate", 
            DisplayOrder = 3 
        },
        new InterviewQuestion { 
            Topic = "C#", 
            Question = "What are async/await and how do they work?", 
            Difficulty = "Advanced", 
            DisplayOrder = 4 
        },
        new InterviewQuestion { 
            Topic = "C#", 
            Question = "What is the difference between .NET Core and .NET Framework?", 
            Difficulty = "Advanced", 
            DisplayOrder = 5 
        },
        
        // Python Questions
        new InterviewQuestion { 
            Topic = "Python", 
            Question = "What are Python decorators and how do you use them?", 
            Difficulty = "Intermediate", 
            DisplayOrder = 1 
        },
        new InterviewQuestion { 
            Topic = "Python", 
            Question = "Explain the difference between lists and tuples.", 
            Difficulty = "Beginner", 
            DisplayOrder = 2 
        },
        new InterviewQuestion { 
            Topic = "Python", 
            Question = "What is the Global Interpreter Lock (GIL)?", 
            Difficulty = "Advanced", 
            DisplayOrder = 3 
        },
        new InterviewQuestion { 
            Topic = "Python", 
            Question = "How do you handle exceptions in Python?", 
            Difficulty = "Intermediate", 
            DisplayOrder = 4 
        },
        new InterviewQuestion { 
            Topic = "Python", 
            Question = "What are list comprehensions and when should you use them?", 
            Difficulty = "Intermediate", 
            DisplayOrder = 5 
        },
        
        // SQL Questions
        new InterviewQuestion { 
            Topic = "SQL", 
            Question = "Explain the difference between INNER JOIN and LEFT JOIN.", 
            Difficulty = "Beginner", 
            DisplayOrder = 1 
        },
        new InterviewQuestion { 
            Topic = "SQL", 
            Question = "What is a primary key and how is it different from a unique key?", 
            Difficulty = "Beginner", 
            DisplayOrder = 2 
        },
        new InterviewQuestion { 
            Topic = "SQL", 
            Question = "Explain the difference between WHERE and HAVING clauses.", 
            Difficulty = "Intermediate", 
            DisplayOrder = 3 
        },
        new InterviewQuestion { 
            Topic = "SQL", 
            Question = "What are indexes and when should you use them?", 
            Difficulty = "Intermediate", 
            DisplayOrder = 4 
        },
        new InterviewQuestion { 
            Topic = "SQL", 
            Question = "What is database normalization?", 
            Difficulty = "Advanced", 
            DisplayOrder = 5 
        },
        
        // JavaScript Questions
        new InterviewQuestion { 
            Topic = "JavaScript", 
            Question = "Explain the difference between let, const, and var.", 
            Difficulty = "Beginner", 
            DisplayOrder = 1 
        },
        new InterviewQuestion { 
            Topic = "JavaScript", 
            Question = "What is closure in JavaScript and how does it work?", 
            Difficulty = "Intermediate", 
            DisplayOrder = 2 
        },
        new InterviewQuestion { 
            Topic = "JavaScript", 
            Question = "Explain the event loop in JavaScript.", 
            Difficulty = "Advanced", 
            DisplayOrder = 3 
        },
        
        // Java Questions
        new InterviewQuestion { 
            Topic = "Java", 
            Question = "Explain the concept of inheritance in Java.", 
            Difficulty = "Beginner", 
            DisplayOrder = 1 
        },
        new InterviewQuestion { 
            Topic = "Java", 
            Question = "What is the difference between abstract class and interface in Java?", 
            Difficulty = "Intermediate", 
            DisplayOrder = 2 
        },
        new InterviewQuestion { 
            Topic = "Java", 
            Question = "How does garbage collection work in Java?", 
            Difficulty = "Advanced", 
            DisplayOrder = 3 
        }
    };
    
    context.InterviewQuestions.AddRange(questions);
    context.SaveChanges();
}