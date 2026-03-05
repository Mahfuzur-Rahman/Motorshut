namespace MotorsHut.BLL.Contracts.Cars;

public sealed class CarEditOperationResultDto
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public Guid? CarId { get; init; }
    public IReadOnlyList<string> DeletedImageUrls { get; init; } = Array.Empty<string>();

    public static CarEditOperationResultDto Success(string message, Guid? carId = null, IReadOnlyList<string>? deletedImageUrls = null)
    {
        return new CarEditOperationResultDto
        {
            Succeeded = true,
            Message = message,
            CarId = carId,
            DeletedImageUrls = deletedImageUrls ?? Array.Empty<string>()
        };
    }

    public static CarEditOperationResultDto Failure(string message, params string[] errors)
    {
        return new CarEditOperationResultDto
        {
            Succeeded = false,
            Message = message,
            Errors = errors
        };
    }
}
