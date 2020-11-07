#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.IO;
using System.Reflection;
using Autofac;
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using BrickScan.WebApi.Images;
using BrickScan.WebApi.Prediction;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ML;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Serilog;

namespace BrickScan.WebApi
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        private static void ConfigureStorageService(IServiceCollection services, bool isDevelopment)
        {
            if (isDevelopment)
            {
                services.AddTransient<IStorageService, LocalFileStorageService>();
            }
            else
            {
                services.AddSingleton<IStorageService, AzureBlobStorageService>();
            }
        }

        private static void ConfigurePredictionEngine(IServiceCollection services, IConfiguration configuration,
            bool isDevelopment)
        {
            if (isDevelopment)
            {
                var modelFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!,
                    configuration["MlModel:Uri"]);
                services.AddPredictionEnginePool<ModelImageInput, ModelImagePrediction>()
                    .FromFile(filePath: modelFilePath, modelName: configuration["MlModel:ModelName"], watchForChanges: true);
            }
            else
            {
                var pollingPeriodInMinutes = configuration.GetValue<int>("MlModel:PollingPeriodInMinutes");
                services.AddPredictionEnginePool<ModelImageInput, ModelImagePrediction>()
                    .FromUri(modelName: configuration["MlModel:ModelName"], uri: 
                        configuration["MlModel:Uri"], 
                        TimeSpan.FromMinutes(pollingPeriodInMinutes));
            }
        }

        private static void ConfigureDbContext(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("BrickScanDbConnectionString");

            services.AddDbContext<DatasetDbContext>(options =>
            {
                options.UseSqlServer(connectionString,
                    sqlServerOptions =>
                    {
                        sqlServerOptions.MigrationsAssembly(typeof(DatasetDbContext).Assembly.GetName().Name);
                    });
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<ImageFileConverter>().As<IImageFileConverter>();
            builder.RegisterType<ImagePredictor>().As<IImagePredictor>();
            builder.RegisterType<DatasetService>().As<IDatasetService>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(options =>
                    {
                        Configuration.Bind("AzureAdB2C", options);
                    },
                    options =>
                    {
                        Configuration.Bind("AzureAdB2C", options);

                    });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.RequiresTrustedUser, policy =>
                    policy.RequireClaim(Claims.UserLevel, "trusted_user", "admin"));
                options.AddPolicy(Policies.RequiresAdmin, policy =>
                    policy.RequireClaim(Claims.UserLevel, "admin"));
            });

            services.AddControllers();
            services.AddMvcCore();
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            });

            ConfigureDbContext(services, Configuration);

            var isDevelopment = Environment.IsDevelopment();

            ConfigurePredictionEngine(services, Configuration, isDevelopment);
            ConfigureStorageService(services, isDevelopment);

            services.AddProblemDetails(opts => { opts.IncludeExceptionDetails = (context, ex) => isDevelopment; });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "BrickScan API", Version = "v1" });
                options.OperationFilter<RemoveVersionFromParameter>();
                options.DocumentFilter<ReplaceVersionWithExactValueInPath>();
                options.DocumentFilter<ReplaceVersionWithExactValueInPath>();
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.XML";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            app.UseDeveloperExceptionPage();
            //}

            app.UseProblemDetails();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "BrickScan API");
            });

            app.UseHttpsRedirection();
            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
