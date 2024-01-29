using ETicaretAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETicaretAPI.Infrastucture
{
    public static class ServiceRegistration
    {
        public static void AddInfrastuctureServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IFileService,IFileService>();
        }
    }
}
