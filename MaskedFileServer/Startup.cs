﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaskedFileServer
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
            String FilePaths = Environment.GetEnvironmentVariable("File_Path");
            bool DeleteOnExpiry = Environment.GetEnvironmentVariable("Delete_On_Expiry") == "TRUE" ? true : false;
            Int32.TryParse(Environment.GetEnvironmentVariable("Expiration_Term"), out int Term);
            String ConString = Environment.GetEnvironmentVariable("ConnectionString");

            services.AddMvc();
            services.AddSingleton<Wotcha>(o => new Wotcha(ConString, FilePaths, DeleteOnExpiry, Term));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
