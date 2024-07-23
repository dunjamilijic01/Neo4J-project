using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Neo4jClient;

namespace Back
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Back", Version = "v1" });
            });
            var client = new BoltGraphClient(new Uri("bolt://localhost:7687"),"neo4j","rootroot");
            client.ConnectAsync();
            services.AddSingleton<IGraphClient>(client);
             services.AddCors(option=>{
                option.AddPolicy("Cors",builder=>{

                    builder.WithOrigins( new string[] {
                        "https://localhost:8080", 
                        "http://localhost:8080",
                        "https://127.0.0.1:8080",
                        "http://127.0.0.1:8080",
                        "https://localhost:5001",
                        "http://localhost:5001",
                        "https://127.0.0.1:5001",
                        "http://127.0.0.1:5001",
                        "http://127.0.0.1:5500",
                        "https://127.0.0.1:5500",
                        "http://localhost:5500",
                        "https://localhost:5500",
                        "http://127.0.0.1:3000",
                        "https://127.0.0.1:3000",
                        "http://localhost:3000",
                        "https://localhost:3000"
                    }).AllowAnyHeader()
                      .AllowAnyMethod();
                });
            }
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Back v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            
            app.UseCors("Cors");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
