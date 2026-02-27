namespace MotorsHut.BLL.Contracts.Cars;

public sealed class CreateCarImageRequestDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}
