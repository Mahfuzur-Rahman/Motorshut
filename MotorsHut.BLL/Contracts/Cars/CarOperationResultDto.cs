namespace MotorsHut.BLL.Contracts.Cars;

public sealed class CarOperationResultDto
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public Guid? CarId { get; init; }

    public static CarOperationResultDto Success(string message, Guid? carId = null)
    {
        return new CarOperationResultDto
        {
            Succeeded = true,
            Message = message,
            CarId = carId
        };
    }

    public static CarOperationResultDto Failure(string message, params string[] errors)
    {
        return new CarOperationResultDto
        {
            Succeeded = false,
            Message = message,
            Errors = errors
        };
    }
}
