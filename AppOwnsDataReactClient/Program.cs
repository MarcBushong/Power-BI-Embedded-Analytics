var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
app.UseHttpsRedirection();

var options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("index.htm");
app.UseDefaultFiles(options);

app.UseStaticFiles();

// .NET 10: Replaces removed UseSpa() — serves index.htm for all unmatched routes
// enabling full SPA client-side routing support
app.MapFallbackToFile("index.htm");

app.Run();
