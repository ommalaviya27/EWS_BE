namespace Domain.EWS.DataModels.Request.Profile
{
    public class UpdateProfileRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string MobileNumber { get; set; }
    }
}
