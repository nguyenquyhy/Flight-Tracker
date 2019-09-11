using FlightTracker.Web.Data;
using FlightTracker.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace FlightTracker.Web
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
            services.Configure<AppSettings>(appSettings => Configuration.GetSection("AppSettings").Bind(appSettings));

            services.AddSingleton<IFlightStorage>(new JsonFileFlightStorage(Path.Combine(Directory.GetCurrentDirectory(), "flights.json")));

            services.AddControllersWithViews().AddJsonOptions(configuration =>
            {
                configuration.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                configuration.JsonSerializerOptions.IgnoreNullValues = true;
            });

            services.AddSignalR();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "Google";
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login";
                })
                .AddOpenIdConnect("Google", o =>
                {
                    o.ClientId = Configuration["Authentication:Google:ClientId"];
                    o.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                    o.ResponseType = OpenIdConnectResponseType.Code;
                    o.Authority = "https://accounts.google.com";
                    o.CallbackPath = "/signin-google";
                    o.Scope.Add("email");
                    o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    o.RemoteSignOutPath = "/signout-google";
                    o.SignedOutCallbackPath = "/signout-callback-google";

                    o.GetClaimsFromUserInfoEndpoint = true;
                    o.SaveTokens = true;

                    o.Events.OnUserInformationReceived = async (context) =>
                    {
                        var id = context.Principal.GetUserId();
                        var name = context.Principal.GetUserName();
                        var email = context.Principal.GetUserEmail();

                        var adminEmail = Configuration["Authentication:Google:AdminEmail"];
                        if (!email.Equals(adminEmail, StringComparison.InvariantCultureIgnoreCase))
                        {
                            var errorMessage = Configuration["Authentication:Restriction:ErrorMessage"];
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                context.Fail(errorMessage);
                            }
                            else
                            {
                                context.Fail($"Please login using a valid account!");
                            }
                            return;
                        }
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");

                endpoints.MapHub<StatusHub>("/Hubs/Status");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
