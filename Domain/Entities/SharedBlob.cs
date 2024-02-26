namespace Domain.Entities;

public class SharedBlob
{
    required public Uri Sas { get; init; }
    required public string Name { get; init; }
}
