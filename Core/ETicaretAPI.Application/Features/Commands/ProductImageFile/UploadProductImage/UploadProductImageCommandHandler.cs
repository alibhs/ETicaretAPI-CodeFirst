using ETicaretAPI.Application.Abstractions.Storage;
using ETicaretAPI.Application.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETicaretAPI.Application.Features.Commands.ProductImageFile.UploadProductImage
{
    public class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommandRequest, UploadProductImageCommandResponse>
    {
        readonly IProductImageWriteRepository _productImageWriteRepository;
        readonly IProductReadRepository _productReadRepository;
        readonly IStorageService _storageService;

        public UploadProductImageCommandHandler(IProductImageWriteRepository productImageWriteRepository, IProductReadRepository productReadRepository, IStorageService storageService)
        {
            _productImageWriteRepository = productImageWriteRepository;
            _productReadRepository = productReadRepository;
            _storageService = storageService;
        }

        public async Task<UploadProductImageCommandResponse> Handle(UploadProductImageCommandRequest request, CancellationToken cancellationToken)
        {
            List<(string fileName, string pathOrContainerName)> result = await _storageService.UploadAsync("photo-images", request.FormFiles);
           Domain.Entities.Product product = await _productReadRepository.GetByIdAsync(request.Id);

            await _productImageWriteRepository.AddRangeAsync(result.Select(r => new Domain.Entities.ProductImageFile
            {
                FileName = r.fileName,
                Path = r.pathOrContainerName,
                Storage = _storageService.StorageName,
                Products = new List<Domain.Entities.Product>() { product }

            }).ToList());

            await _productImageWriteRepository.SaveAsync();

            return new();

        }
    }
}
