namespace Shared.EWS.Entities
{
    public class PublicHoliday : BaseEntity
    {
        public int Id { get; set; }
        public DateTime HolidayDate { get; set; }
        public required string Name { get; set; }
    }
}