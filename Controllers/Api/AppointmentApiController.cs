using HMS.Data;
using HMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("slots")]
        public async Task<IActionResult> GetSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var appointmentDate))
                return BadRequest(new { error = "Invalid date format." });

            var dayName = appointmentDate.DayOfWeek.ToString();

            var availability = await _context.Availabilities
                .Where(a => a.DoctorId == doctorId && a.DayOfWeek == dayName && a.IsActive)
                .FirstOrDefaultAsync();

            if (availability == null)
                return Ok(new { slots = Array.Empty<string>(), message = "Doctor is not available on this day." });

            var bookedSlots = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == appointmentDate.Date && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.TimeSlot)
                .ToListAsync();

            var allSlots = new List<string>();
            var current = availability.StartTime;
            while (current.Add(TimeSpan.FromMinutes(30)) <= availability.EndTime)
            {
                var hour = current.Hours;
                var minute = current.Minutes;
                var ampm = hour >= 12 ? "PM" : "AM";
                var displayHour = hour > 12 ? hour - 12 : (hour == 0 ? 12 : hour);
                var slotLabel = $"{displayHour:D2}:{minute:D2} {ampm}";

                if (!bookedSlots.Contains(slotLabel))
                    allSlots.Add(slotLabel);

                current = current.Add(TimeSpan.FromMinutes(30));
            }

            return Ok(new { slots = allSlots });
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<IActionResult> GetDoctor(int doctorId)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (doctor == null) return NotFound();

            return Ok(new
            {
                doctor.DoctorId,
                Name = doctor.User?.FullName,
                doctor.Specialization,
                doctor.Qualification,
                doctor.ExperienceYears,
                doctor.ConsultationFee,
                doctor.Bio
            });
        }
    }
}
