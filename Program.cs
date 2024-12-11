


using FileMonitor.Core.Actions;
using FileMonitor.Infrastructure.Managers;

var builder = WebApplication.CreateSlimBuilder(args);

//builder.Services.ConfigureHttpJsonOptions(options =>
//{
//    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
//});

// Ajout du gestionnaire de jobs
builder.Services.AddSingleton<IJobManager, JobManager>();

// Ajout du pipeline d'actions
builder.Services.AddSingleton<FileActionPipeline>();
builder.Services.AddTransient<IFileAction, LogFileAction>();
builder.Services.AddTransient<IFileAction, TransformFileAction>();

//ajout de l'abstration des monitors
builder.Services.AddScoped<MonitorFactory>();
//builder.Services.AddSingleton<IMonitorFactory, MonitorFactory>();
//builder.Services.AddScoped<LocalMonitor>();
//builder.Services.AddScoped<SmbMonitor>();
//builder.Services.AddScoped<SftpMonitor>();

//ajout de l'abstration des handlers de fichiers
builder.Services.AddSingleton<IFileSystemHandlerFactory, FileSystemHandlerFactory>();
builder.Services.AddTransient<SftpFileSystemHandler>();
builder.Services.AddTransient<SmbFileSystemHandler>();
builder.Services.AddTransient<LocalFileSystemHandler>();

// Ajout des services sp�cifiques n�cessaires pour chaque handler
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddScoped<SftpFolderDescriptor>();
builder.Services.AddScoped<SmbFolderDescriptor>(); 
builder.Services.AddScoped<LocalFolderDescriptor>();


var app = builder.Build();



app.Run();


