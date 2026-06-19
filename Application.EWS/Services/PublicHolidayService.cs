using Application.EWS.Interfaces;
using AutoMapper;
using Domain.EWS.DataModels.Request.PublicHoliday;
using Domain.EWS.DataModels.Response.PublicHoliday;
using Domain.EWS.Interface;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class PublicHolidayService(
        IPublicHolidayRepository repository,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<PublicHoliday>(repository, principal), IPublicHolidayService
    {
        private readonly IPublicHolidayRepository _holidayRepository = repository;
        private readonly IMapper _mapper = mapper;

        private bool IsAdmin => CurrentRoleId == 1;

        public async Task<PagedResponse<HolidayResponse>> GetHolidaysAsync(PaginationRequest pagination)
        {
            var paged = await _holidayRepository.GetPagedAsync(pagination);

            return PagedResponse<HolidayResponse>.Create(
                paged.Items.Select(_mapper.Map<HolidayResponse>).ToList(),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize);
        }

        public async Task<HolidayResponse> GetHolidayByIdAsync(int id)
        {
            var holiday = await _holidayRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Public holiday with id '{id}' was not found.");

            return _mapper.Map<HolidayResponse>(holiday);
        }

        public async Task<HolidayResponse> CreateAsync(CreateHolidayRequest request)
        {
            if (!IsAdmin)
                throw new ForbiddenException("Only Admins can create public holidays.");

            var holidayDate = DateTime.SpecifyKind(request.HolidayDate.Date, DateTimeKind.Utc);

            var duplicate = await _holidayRepository.ExistsForDateAsync(holidayDate);
            if (duplicate)
                throw new DuplicateRecordException($"A public holiday already exists on {holidayDate:yyyy-MM-dd}.");

            var entity = new PublicHoliday
            {
                Name = request.Name.Trim(),
                HolidayDate = holidayDate
            };

            await _repository.AddAsync(entity);

            return _mapper.Map<HolidayResponse>(entity);
        }

        public async Task<HolidayResponse> UpdateAsync(int id, UpdateHolidayRequest request)
        {
            if (!IsAdmin)
                throw new ForbiddenException("Only Admins can update public holidays.");

            var holiday = await _holidayRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Public holiday with id '{id}' was not found.");

            var holidayDate = DateTime.SpecifyKind(request.HolidayDate.Date, DateTimeKind.Utc);

            if (holiday.HolidayDate != holidayDate)
            {
                var duplicate = await _holidayRepository.ExistsForDateAsync(holidayDate, id);
                if (duplicate)
                    throw new DuplicateRecordException($"A public holiday already exists on {holidayDate:yyyy-MM-dd}.");
            }

            holiday.Name = request.Name.Trim();
            holiday.HolidayDate = holidayDate;

            await _repository.UpdateAsync(holiday);

            return _mapper.Map<HolidayResponse>(holiday);
        }

        public async Task DeleteAsync(int id)
        {
            if (!IsAdmin)
                throw new ForbiddenException("Only Admins can delete public holidays.");

            _ = await _holidayRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Public holiday with id '{id}' was not found.");

            await _repository.DeleteAsync(id);
        }
    }
}