using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MotorsHut.BLL.Abstractions.Services;
using MotorsHut.Models;
using MotorsHut.Models.Public;

namespace MotorsHut.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICarService _carService;

        public HomeController(ILogger<HomeController> logger, ICarService carService)
        {
            _logger = logger;
            _carService = carService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("cars")]
        public async Task<IActionResult> Showcase(CancellationToken cancellationToken)
        {
            const string fallbackImageUrl = "/uploads/cars/no-image.svg";

            var list = await _carService.SearchAsync(null, cancellationToken);
            if (list.Count == 0)
            {
                return View(new CarShowcaseViewModel());
            }

            var detailTasks = list
                .Select(car => _carService.GetByIdAsync(car.Id, cancellationToken))
                .ToArray();
            var details = await Task.WhenAll(detailTasks);

            var cards = details
                .Where(car => car is not null)
                .Select(car => car!)
                .Select(car =>
                {
                    var orderedImages = car.Images
                        .OrderBy(i => i.SortOrder)
                        .ThenBy(i => i.Id)
                        .Select(i => i.ImageUrl)
                        .Where(url => !string.IsNullOrWhiteSpace(url))
                        .ToArray();

                    var cardImage = car.Images
                        .Where(i => !string.IsNullOrWhiteSpace(i.ImageUrl))
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault() ?? fallbackImageUrl;

                    return new CarShowcaseItemViewModel
                    {
                        Id = car.Id,
                        Title = $"{car.Make} {car.Model}".Trim(),
                        Variant = car.Variant,
                        Year = car.Year,
                        Price = car.Price,
                        MileageKm = car.MileageKm,
                        Color = car.Color,
                        Vin = car.Vin,
                        IsSold = car.IsSold,
                        IsReturned = car.IsReturned,
                        CardImageUrl = cardImage,
                        ImageUrls = orderedImages.Length > 0 ? orderedImages : [fallbackImageUrl]
                    };
                })
                .OrderByDescending(c => c.Year)
                .ThenBy(c => c.Title)
                .ToArray();

            return View(new CarShowcaseViewModel { Cars = cards });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
