using ETicaretAPI.Application.Abstractions.Storage;
using ETicaretAPI.Application.Repositories;
using ETicaretAPI.Application.RequestParameters;
using ETicaretAPI.Application.ViewModels.Products;
using ETicaretAPI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ETicaretAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        readonly private IProductWriteRepository _productWriteRepository;
        readonly private IProductReadRepository _productReadRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        readonly IFileWriteRepository _fileWriteRepository;
        readonly IFileReadRepository _fileReadRepository;
        readonly IProductImageReadRepository _productImageReadRepository;
        readonly IProductImageWriteRepository _productImageWriteRepository;
        readonly IInvoiceFileReadRepository? _invoiceFileReadRepository;
        readonly IInvoiceFileWriteRepository? _invoiceFileWriteRepository;
        readonly IStorageService? _storageService;
        readonly IConfiguration? _configuration;

        public ProductsController(IProductWriteRepository productWriteRepository, IProductReadRepository productReadRepository, IWebHostEnvironment webHostEnvironment, IFileWriteRepository fileWriteRepository, IFileReadRepository fileReadRepository, IProductImageReadRepository productImageReadRepository, IProductImageWriteRepository productImageWriteRepository, IInvoiceFileReadRepository? invoiceFileReadRepository, IInvoiceFileWriteRepository? invoiceFileWriteRepository, IStorageService? storageService, IConfiguration? configuration)
        {
            _productWriteRepository = productWriteRepository;
            _productReadRepository = productReadRepository;
            _webHostEnvironment = webHostEnvironment;
            _fileWriteRepository = fileWriteRepository;
            _fileReadRepository = fileReadRepository;
            _productImageReadRepository = productImageReadRepository;
            _productImageWriteRepository = productImageWriteRepository;
            _invoiceFileReadRepository = invoiceFileReadRepository;
            _invoiceFileWriteRepository = invoiceFileWriteRepository;
            _storageService = storageService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] Pagination pagination)
        {
            var totalCount = _productReadRepository.GetAll(false).Count();
            var products = _productReadRepository.GetAll(false).Select(p => new
            {
                p.Id,
                p.Name,
                p.Stock,
                p.Price,
                p.CreatedDate,
                p.UpdatedDate
            }).Skip(pagination.Page * pagination.Size).Take(pagination.Size);

            return Ok(new { totalCount, products });
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            Product product = await _productReadRepository.GetByIdAsync(id, false);
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Post(VM_Create_Product model)
        {
            if (ModelState.IsValid)
            {

            }
            await _productWriteRepository.AddAsync(new()
            {
                Name = model.Name,
                Stock = model.Stock,
                Price = model.Price,
            });
            await _productWriteRepository.SaveAsync();
            return StatusCode((int)HttpStatusCode.Created);
        }

        [HttpPut]
        public async Task<IActionResult> Put(VM_Update_Product model)
        {
            Product product = await _productReadRepository.GetByIdAsync(model.Id);
            product.Stock = model.Stock;
            product.Name = model.Name;
            product.Price = model.Price;
            await _productWriteRepository.SaveAsync();
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            await _productWriteRepository.RemoveAsync(id);
            await _productWriteRepository.SaveAsync();
            return Ok(new
            {
                message = "Silme işlemi başarılı"
            });

        }
        //[HttpPost("[action]")]
        //public async Task<ActionResult> Upload()
        //{
        //    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "resorce/product-images");

        //    if(!Directory.Exists(uploadPath))
        //    {
        //        Directory.CreateDirectory(uploadPath);
        //    }

        //    Random r = new();


        //    foreach (IFormFile file in Request.Form.Files)
        //    {
        //        string fullPath = Path.Combine(uploadPath, $"{r.Next()}{Path.GetExtension(file.FileName)}");

        //        using FileStream fileStream = new(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, useAsync: false);
        //        await file.CopyToAsync(fileStream);
        //        await fileStream.FlushAsync();
        //    }
        //    return Ok();
        //}

        [HttpPost("[action]")]
        public async Task<IActionResult> Upload(string id)
        {
            List<(string fileName, string pathOrContainerName)> result = await _storageService.UploadAsync("photo-images", Request.Form.Files);

            Product product = await _productReadRepository.GetByIdAsync(id);

            await _productImageWriteRepository.AddRangeAsync(result.Select(r => new ProductImageFile
            {
                FileName = r.fileName,
                Path = r.pathOrContainerName,
                Storage = _storageService.StorageName,
                Products = new List<Product>() { product }

            }).ToList());

            await _productImageWriteRepository.SaveAsync();

            return Ok();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetProductImages(string id)
        {
            Product? product = await _productReadRepository.Table.Include(p => p.ProductImageFiles)
                 .FirstOrDefaultAsync(p => p.Id == Guid.Parse(id));

            return Ok(product.ProductImageFiles.Select(p => new
            {
                Path = $"{_configuration["BaseStorageUrl"]}/{p.Path}", //ürünlerin resmini çekerken azuredan gelenlerin başına url ekledik.
                p.FileName,
                p.Id
            }));
        }

        [HttpDelete("[action]/{id}")]
        public async Task<IActionResult> DeleteProductImage(string id, string imageId)
        {
            Product? product = await _productReadRepository.Table.Include(p => p.ProductImageFiles)
                  .FirstOrDefaultAsync(p => p.Id == Guid.Parse(id));

            ProductImageFile productImageFile = product.ProductImageFiles.FirstOrDefault(p => p.Id == Guid.Parse(imageId));
            product.ProductImageFiles.Remove(productImageFile);
            await _productWriteRepository.SaveAsync();
            return Ok();
        }

    }
}
