namespace TB.DanceDance.API.Models
{
    public record RequestEventAssigmentModel
    {
        public ICollection<string>? Events{ get; set; }
        public ICollection<string>? Groups { get; set; }
    }
}
