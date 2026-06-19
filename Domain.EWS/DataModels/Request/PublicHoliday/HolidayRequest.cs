namespace Domain.EWS.DataModels.Request.PublicHoliday
{
    public class CreateHolidayRequest
    {
        public DateTime HolidayDate { get; set; }
        public required string Name { get; set; }
    }

    public class UpdateHolidayRequest
    {
        public DateTime HolidayDate { get; set; }
        public required string Name { get; set; }
    }
}