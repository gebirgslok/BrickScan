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
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using BrickScan.WebApi.Images;
using BrickScan.WebApi.Prediction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ML;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

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

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddMvcCore();
            services.AddApiVersioning(options => options.ReportApiVersions = true);

            var modelFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!,
                Configuration.GetValue<string>("ModelFilePath"));

            services.AddPredictionEnginePool<ModelInput, ModelOutput>()
                .FromFile(filePath: modelFilePath, modelName: "BrickScanModel", watchForChanges: true);

            services.AddTransient<IImageFileConverter, ImageFileConverter>();
            services.AddTransient<IImagePredictor, ImagePredictor>();
            services.AddTransient<IDatasetService, DatasetService>();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<DatasetDbContext>(options =>
            {
                options.UseSqlServer(connectionString,
                    x => x.MigrationsAssembly(typeof(DatasetDbContext).Assembly.GetName().Name));
            });

            if (Environment.IsDevelopment())
            {
                services.AddTransient<IStorageService, LocalFileStorageService>();
            }
            else
            {
                services.AddTransient<IStorageService, LocalFileStorageService>();
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DatasetDbContext context)
        {
            //if (env.IsDevelopment())
            //{
            app.UseDeveloperExceptionPage();
            //}

            //using var context = app.ApplicationServices.GetService<DatasetDbContext>();
            context.Database.Migrate();

            app.UseHttpsRedirection();
            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
