namespace MotorsHut.BLL.Contracts.Cars;

public sealed class CarImageItemDto
{
    public Guid Id { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}
