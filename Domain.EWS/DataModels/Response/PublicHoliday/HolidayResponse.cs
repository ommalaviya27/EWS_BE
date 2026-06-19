namespace Domain.EWS.DataModels.Response.PublicHoliday
{
    public class HolidayResponse
    {
        public int Id { get; set; }
        public DateTime HolidayDate { get; set; }
        public required string Name { get; set; }
    }
}