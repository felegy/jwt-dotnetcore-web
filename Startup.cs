using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JwtApp
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
            System.Net.Cache.RequestCacheLevel cacheLevel = System.Net.Cache.RequestCacheLevel.Default;
            Enum.TryParse<System.Net.Cache.RequestCacheLevel>(Configuration["Jwt:JwkCacheLevel"], out cacheLevel);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeyResolver =
                        (s, securityToken, identifier, parameters) =>
                        {
                            // Get JsonWebKeySet from Issuer https://www.rfc-editor.org/rfc/rfc7517
                            // https://developers.cloudflare.com/cloudflare-one/identity/users/access-jwt/validating-json/
                            using (var webClient = new WebClient())
                            {
                                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(cacheLevel);
                                webClient.BaseAddress = parameters.ValidIssuer;
                                return JsonWebKeySet.Create(webClient.DownloadString(Configuration["Jwt:JwkEndpoint"])).Keys;
                            }
                        },
                        ValidateIssuer = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = Configuration["Jwt:Audience"],
                        ValidateLifetime = true
                    };
                    options.Events = new JwtBearerEvents()
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.HttpContext.Request.Headers[Configuration["Jwt:Header"]];
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddMvc();
            services.AddControllers();
            services.AddRazorPages();
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

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
