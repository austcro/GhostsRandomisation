// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System;
using Ghosts.Api.Infrastructure;
using Ghosts.Api.Infrastructure.Data;
using Ghosts.Api.Models;
using Ghosts.Domain.Code;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;

namespace Ghosts.Api
{
    public class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static ApiDetails.ClientOptions ClientConfig { get; set; }
        public static ApiDetails.InitOptions InitConfig { get; set; }

        public static void Main(string[] args)
        {
            Console.WriteLine(ApplicationDetails.Header);
            log.Warn("GHOSTS API coming online...");

            ApiDetails.LoadConfiguration();

            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
            
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var dbInitializerLogger = services.GetRequiredService<ILogger<DbInitializer>>();

                    DbInitializer.Initialize(context, userManager, roleManager, dbInitializerLogger).Wait();
                }
                catch (Exception ex)
                {
                    log.Fatal(ex, "An error occurred while seeding the GHOSTS database");
                }
            }

            host.Run();
        }
    }
}