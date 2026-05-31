using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TB.DanceDance.Access.Domain.Entities;

public class Group
{
    private Group() { }
    
    public Guid Id { get; set; }
    public required string Name { get; set; }
    
    public required DateOnly SeasonStart { get; set; }
    public required DateOnly SeasonEnd { get; set; }
}

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
    }
}