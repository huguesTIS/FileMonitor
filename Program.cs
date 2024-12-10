


using FileMonitor.Core.Actions;

var builder = WebApplication.CreateSlimBuilder(args);

//builder.Services.ConfigureHttpJsonOptions(options =>
//{
//    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
//});

// Ajout du pipeline d'actions
builder.Services.AddSingleton<FileActionPipeline>();
builder.Services.AddTransient<IFileAction, LogFileAction>();
builder.Services.AddTransient<IFileAction, TransformFileAction>();

//ajout de l'abstration des monitors
//builder.Services.AddSingleton<IMonitorFactory, MonitorFactory>();
//builder.Services.AddScoped<LocalMonitor>();
//builder.Services.AddScoped<SmbMonitor>();
//builder.Services.AddScoped<SftpMonitor>();

//ajout de l'abstration des handlers de fichiers
builder.Services.AddSingleton<FileSystemHandlerFactory>();
builder.Services.AddScoped<LocalFileSystemHandler>();
builder.Services.AddScoped<SmbFileSystemHandler>();
builder.Services.AddScoped<SftpFileSystemHandler>();

// Ajout des services spécifiques nécessaires pour chaque handler
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddScoped<SftpFolderDescriptor>();
builder.Services.AddScoped<SmbFolderDescriptor>(); 
builder.Services.AddScoped<LocalFolderDescriptor>();


var app = builder.Build();



app.Run();


