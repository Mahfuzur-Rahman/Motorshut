namespace MotorsHut.BLL.Contracts.Cars;

public sealed class UpdateCarImageSortOrderRequestDto
{
    public Guid ImageId { get; init; }
    public int SortOrder { get; init; }
}
