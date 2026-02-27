using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorsHut.BLL.Abstractions.Services;
using MotorsHut.BLL.Contracts.Auth;
using MotorsHut.BLL.Contracts.Cars;
using MotorsHut.Models.Admin;

namespace MotorsHut.Controllers;

[Authorize(Roles = "SuperAdmin,Admin")]
[Route("admin")]
public sealed class AdminController : Controller
{
    private const int MaxImagesPerCar = 10;
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly IAuthService _authService;
    private readonly ICarService _carService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(IAuthService authService, ICarService carService, IWebHostEnvironment webHostEnvironment)
    {
        _authService = authService;
        _carService = carService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("cars")]
    public async Task<IActionResult> Cars(string? search, CancellationToken cancellationToken)
    {
        var cars = await _carService.SearchAsync(search, cancellationToken);
        var model = new CarsListViewModel
        {
            Search = search,
            Cars = cars.Select(c => new CarListItemViewModel
            {
                Id = c.Id,
                Make = c.Make,
                Model = c.Model,
                Variant = c.Variant,
                Year = c.Year,
                Price = c.Price,
                MileageKm = c.MileageKm,
                IsSold = c.IsSold,
                IsReturned = c.IsReturned
            }).ToArray()
        };

        return View(model);
    }

    [HttpGet("cars/add")]
    public IActionResult AddCar()
    {
        return View(new CarFormViewModel());
    }

    [HttpPost("cars/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCar(CarFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _carService.CreateAsync(new CreateCarRequestDto
        {
            Make = form.Make,
            Model = form.Model,
            Variant = form.Variant,
            Year = form.Year,
            Price = form.Price,
            MileageKm = form.MileageKm,
            Color = form.Color,
            Vin = form.Vin,
            IsSold = form.IsSold,
            IsReturned = form.IsReturned
        }, cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
            ModelState.AddModelError(string.Empty, errors);
            return View(form);
        }

        var createdCarId = result.CarId ?? Guid.Empty;
        if (createdCarId != Guid.Empty && form.NewImages.Count > 0)
        {
            var uploadResult = await SaveUploadedImagesAsync(createdCarId, form.NewImages, cancellationToken);
            if (uploadResult.Errors.Count > 0)
            {
                foreach (var error in uploadResult.Errors)
                {
                    ModelState.AddModelError(nameof(form.NewImages), error);
                }

                return View(form);
            }

            if (uploadResult.Images.Count > 0)
            {
                var addImagesResult = await _carService.AddImagesAsync(createdCarId, uploadResult.Images, cancellationToken);
                if (!addImagesResult.Succeeded)
                {
                    foreach (var savedPath in uploadResult.SavedPhysicalPaths)
                    {
                        DeletePhysicalFile(savedPath);
                    }

                    var errors = addImagesResult.Errors.Count > 0 ? string.Join(" ", addImagesResult.Errors) : addImagesResult.Message;
                    ModelState.AddModelError(nameof(form.NewImages), errors);
                    return View(form);
                }
            }
        }

        TempData["SuccessMessage"] = "Car added successfully.";
        return RedirectToAction(nameof(Cars));
    }

    [HttpGet("cars/edit/{id:guid}")]
    public async Task<IActionResult> EditCar(Guid id, CancellationToken cancellationToken)
    {
        var car = await _carService.GetByIdAsync(id, cancellationToken);
        if (car is null)
        {
            return NotFound();
        }

        var model = new CarFormViewModel
        {
            Id = car.Id,
            Make = car.Make,
            Model = car.Model,
            Variant = car.Variant,
            Year = car.Year,
            Price = car.Price,
            MileageKm = car.MileageKm,
            Color = car.Color,
            Vin = car.Vin,
            IsSold = car.IsSold,
            IsReturned = car.IsReturned,
            ExistingImages = car.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new CarImageViewModel
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                })
                .ToArray(),
            SelectedPrimaryImageId = car.Images.FirstOrDefault(i => i.IsPrimary)?.Id
        };

        return View(model);
    }

    [HttpPost("cars/edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCar(Guid id, CarFormViewModel form, CancellationToken cancellationToken)
    {
        var existingCar = await _carService.GetByIdAsync(id, cancellationToken);
        if (existingCar is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateExistingImagesAsync(form, id, cancellationToken);
            form.Id = id;
            return View(form);
        }

        var result = await _carService.UpdateAsync(new UpdateCarRequestDto
        {
            Id = id,
            Make = form.Make,
            Model = form.Model,
            Variant = form.Variant,
            Year = form.Year,
            Price = form.Price,
            MileageKm = form.MileageKm,
            Color = form.Color,
            Vin = form.Vin,
            IsSold = form.IsSold,
            IsReturned = form.IsReturned
        }, cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
            ModelState.AddModelError(string.Empty, errors);
            await PopulateExistingImagesAsync(form, id, cancellationToken);
            form.Id = id;
            return View(form);
        }

        var deleteIds = form.DeleteImageIds.Distinct().ToHashSet();
        var existingRemainsCount = existingCar.Images.Count(i => !deleteIds.Contains(i.Id));
        if (existingRemainsCount + form.NewImages.Count > MaxImagesPerCar)
        {
            ModelState.AddModelError(nameof(form.NewImages), $"A car can have up to {MaxImagesPerCar} images.");
            await PopulateExistingImagesAsync(form, id, cancellationToken);
            form.Id = id;
            return View(form);
        }

        foreach (var imageId in deleteIds)
        {
            var deleteResult = await _carService.RemoveImageAsync(id, imageId, cancellationToken);
            if (!deleteResult.Succeeded)
            {
                var errors = deleteResult.Errors.Count > 0 ? string.Join(" ", deleteResult.Errors) : deleteResult.Message;
                ModelState.AddModelError(string.Empty, errors);
                await PopulateExistingImagesAsync(form, id, cancellationToken);
                form.Id = id;
                return View(form);
            }

            var deletedImage = existingCar.Images.FirstOrDefault(i => i.Id == imageId);
            if (deletedImage is not null)
            {
                DeletePhysicalFileByUrl(deletedImage.ImageUrl);
            }
        }

        if (form.NewImages.Count > 0)
        {
            var uploadResult = await SaveUploadedImagesAsync(id, form.NewImages, cancellationToken);
            if (uploadResult.Errors.Count > 0)
            {
                foreach (var error in uploadResult.Errors)
                {
                    ModelState.AddModelError(nameof(form.NewImages), error);
                }

                await PopulateExistingImagesAsync(form, id, cancellationToken);
                form.Id = id;
                return View(form);
            }

            if (uploadResult.Images.Count > 0)
            {
                var addImagesResult = await _carService.AddImagesAsync(id, uploadResult.Images, cancellationToken);
                if (!addImagesResult.Succeeded)
                {
                    foreach (var savedPath in uploadResult.SavedPhysicalPaths)
                    {
                        DeletePhysicalFile(savedPath);
                    }

                    var errors = addImagesResult.Errors.Count > 0 ? string.Join(" ", addImagesResult.Errors) : addImagesResult.Message;
                    ModelState.AddModelError(nameof(form.NewImages), errors);
                    await PopulateExistingImagesAsync(form, id, cancellationToken);
                    form.Id = id;
                    return View(form);
                }
            }
        }

        if (form.SelectedPrimaryImageId.HasValue && !deleteIds.Contains(form.SelectedPrimaryImageId.Value))
        {
            var primaryResult = await _carService.SetPrimaryImageAsync(id, form.SelectedPrimaryImageId.Value, cancellationToken);
            if (!primaryResult.Succeeded)
            {
                var errors = primaryResult.Errors.Count > 0 ? string.Join(" ", primaryResult.Errors) : primaryResult.Message;
                ModelState.AddModelError(string.Empty, errors);
                await PopulateExistingImagesAsync(form, id, cancellationToken);
                form.Id = id;
                return View(form);
            }
        }

        TempData["SuccessMessage"] = "Car and images updated successfully.";
        return RedirectToAction(nameof(Cars));
    }

    [HttpPost("cars/edit/{id:guid}/images/{imageId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCarImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var car = await _carService.GetByIdAsync(id, cancellationToken);
        if (car is null)
        {
            return NotFound();
        }

        var image = car.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            TempData["ErrorMessage"] = "Image not found.";
            return RedirectToAction(nameof(EditCar), new { id });
        }

        var result = await _carService.RemoveImageAsync(id, imageId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
            return RedirectToAction(nameof(EditCar), new { id });
        }

        DeletePhysicalFileByUrl(image.ImageUrl);
        TempData["SuccessMessage"] = "Image deleted successfully.";
        return RedirectToAction(nameof(EditCar), new { id });
    }

    [HttpPost("cars/edit/{id:guid}/images/{imageId:guid}/primary")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryCarImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var result = await _carService.SetPrimaryImageAsync(id, imageId, cancellationToken);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Primary image updated.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
        }

        return RedirectToAction(nameof(EditCar), new { id });
    }

    [HttpGet("register-admin")]
    public IActionResult RegisterAdmin()
    {
        return View(new RegisterAdminViewModel());
    }

    [HttpPost("register-admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterAdmin(RegisterAdminViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAdminAsync(new RegisterRequestDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            UserName = model.UserName,
            Email = model.Email,
            Password = model.Password
        }, cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
            ModelState.AddModelError(string.Empty, errors);
            return View(model);
        }

        TempData["SuccessMessage"] = "New admin account created successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    private async Task PopulateExistingImagesAsync(CarFormViewModel form, Guid id, CancellationToken cancellationToken)
    {
        var car = await _carService.GetByIdAsync(id, cancellationToken);
        form.ExistingImages = car?.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new CarImageViewModel
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                IsPrimary = i.IsPrimary
            })
            .ToArray() ?? Array.Empty<CarImageViewModel>();
        form.SelectedPrimaryImageId ??= car?.Images.FirstOrDefault(i => i.IsPrimary)?.Id;
    }

    private async Task<(List<CreateCarImageRequestDto> Images, List<string> SavedPhysicalPaths, List<string> Errors)> SaveUploadedImagesAsync(
        Guid carId,
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var savedPhysicalPaths = new List<string>();
        var images = new List<CreateCarImageRequestDto>();

        var validFiles = files.Where(f => f.Length > 0).ToArray();
        if (validFiles.Length == 0)
        {
            return (images, savedPhysicalPaths, errors);
        }

        if (validFiles.Length > MaxImagesPerCar)
        {
            errors.Add($"You can upload up to {MaxImagesPerCar} images at once.");
            return (images, savedPhysicalPaths, errors);
        }

        foreach (var file in validFiles)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (!AllowedImageExtensions.Contains(extension))
            {
                errors.Add($"'{file.FileName}' has an unsupported format. Allowed: jpg, jpeg, png, webp.");
            }

            if (file.Length > MaxImageSizeBytes)
            {
                errors.Add($"'{file.FileName}' exceeds 5 MB.");
            }
        }

        if (errors.Count > 0)
        {
            return (images, savedPhysicalPaths, errors);
        }

        var rootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var relativeFolder = Path.Combine("uploads", "cars", carId.ToString("N"));
        var physicalFolder = Path.Combine(rootPath, relativeFolder);
        Directory.CreateDirectory(physicalFolder);

        foreach (var file in validFiles)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var generatedName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(physicalFolder, generatedName);
            await using var stream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            savedPhysicalPaths.Add(physicalPath);
            var relativeUrl = "/" + Path.Combine(relativeFolder, generatedName).Replace("\\", "/");
            images.Add(new CreateCarImageRequestDto
            {
                ImageUrl = relativeUrl
            });
        }

        return (images, savedPhysicalPaths, errors);
    }

    private void DeletePhysicalFileByUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var rootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(rootPath, relativePath);
        DeletePhysicalFile(physicalPath);
    }

    private static void DeletePhysicalFile(string physicalPath)
    {
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }
}
