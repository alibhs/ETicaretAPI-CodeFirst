using ETicaretAPI.Application.Abstractions.Storage;
using ETicaretAPI.Infrastucture.Services;
using ETicaretAPI.Infrastucture.Services.Storage;
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
            serviceCollection.AddScoped<IStorageService, StorageService>();
        }
        public static void AddStorage<T>(this IServiceCollection serviceCollection) where T : Storage , IStorage
        {
            //Azure Aws Local Gibi kullanılan serviese göre program.cs deki builderı tetiklemeyi sağlar.
            serviceCollection.AddScoped<IStorage, T>(); 
        }

    }
}
