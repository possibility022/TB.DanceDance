namespace TB.DanceDance.API.Models
{
    public record RequestEventAssigmentModel
    {
        public ICollection<Guid>? Events{ get; set; }
        public ICollection<Guid>? Groups { get; set; }
    }
}
