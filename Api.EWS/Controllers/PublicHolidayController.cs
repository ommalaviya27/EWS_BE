using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.PublicHoliday;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.DataModel.Request;
using Shared.EWS.Helpers;
using System.Net;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/public-holidays")]
    public class PublicHolidayController(IPublicHolidayService holidayService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination)
        {
            var result = await holidayService.GetHolidaysAsync(pagination);
            return Ok(ResponseHelper.SuccessResponse(result, "Public holidays fetched successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await holidayService.GetHolidayByIdAsync(id);
            return Ok(ResponseHelper.SuccessResponse(result, "Public holiday fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHolidayRequest request)
        {
            var result = await holidayService.CreateAsync(request);
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Public holiday created successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateHolidayRequest request)
        {
            var result = await holidayService.UpdateAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Public holiday updated successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await holidayService.DeleteAsync(id);
            return Ok(ResponseHelper.SuccessResponse<object>(null!, "Public holiday deleted successfully."));
        }
    }
}