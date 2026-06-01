using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Management;

class AddOrUpdateUserHandler : IRequestHandler<AddOrUpdateUserCommand, bool>
{
    private readonly AccessDbContext dbContext;

    public AddOrUpdateUserHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> HandleAsync(AddOrUpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        var record = dbContext.Users.Find(request.Id);
        if (record != null)
        {
            record.FirstName = request.FirstName;
            record.LastName = request.LastName;
            record.Email = request.Email;
        }
        else
        {
            dbContext.Users.Add(User.Factory.Create(request.Id, request.FirstName, request.LastName, request.Email));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
