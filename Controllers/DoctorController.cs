using HMS.Data;
using HMS.Models;
using HMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMS.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Doctor?> GetCurrentDoctor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == user.Id);
        }

        public async Task<IActionResult> Dashboard()
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                TotalAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctor.DoctorId),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctor.DoctorId && a.Status == AppointmentStatus.Pending),
                TodayAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctor.DoctorId && a.AppointmentDate.Date == DateTime.Today),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctor.DoctorId && a.Status == AppointmentStatus.Completed),
                RecentAppointments = await _context.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p!.User)
                    .Where(a => a.DoctorId == doctor.DoctorId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(10)
                    .ToListAsync()
            };

            ViewBag.DoctorName = doctor.User?.FullName;
            return View(model);
        }

        public async Task<IActionResult> Appointments()
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return RedirectToAction("Login", "Account");

            var appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Prescription)
                .Where(a => a.DoctorId == doctor.DoctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id && a.DoctorId == doctor.DoctorId);
            if (appointment != null && Enum.TryParse<AppointmentStatus>(status, out var newStatus))
            {
                appointment.Status = newStatus;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment status updated.";
            }
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> Availability()
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return RedirectToAction("Login", "Account");

            var availabilities = await _context.Availabilities
                .Where(a => a.DoctorId == doctor.DoctorId)
                .OrderBy(a => a.DayOfWeek)
                .ToListAsync();

            return View(availabilities);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAvailability(List<AvailabilityViewModel> slots)
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return RedirectToAction("Login", "Account");

            var existing = await _context.Availabilities.Where(a => a.DoctorId == doctor.DoctorId).ToListAsync();
            _context.Availabilities.RemoveRange(existing);

            foreach (var slot in slots)
            {
                if (TimeSpan.TryParse(slot.StartTime, out var start) && TimeSpan.TryParse(slot.EndTime, out var end))
                {
                    _context.Availabilities.Add(new Availability
                    {
                        DoctorId = doctor.DoctorId,
                        DayOfWeek = slot.DayOfWeek,
                        StartTime = start,
                        EndTime = end,
                        IsActive = slot.IsActive
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Availability updated successfully.";
            return RedirectToAction("Availability");
        }

        [HttpGet]
        public async Task<IActionResult> Prescription(int appointmentId)
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Prescription)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId);

            if (appointment == null) return NotFound();

            ViewBag.Appointment = appointment;

            if (appointment.Prescription != null)
            {
                return View(new PrescriptionViewModel
                {
                    AppointmentId = appointmentId,
                    Medication = appointment.Prescription.Medication,
                    Dosage = appointment.Prescription.Dosage,
                    Instructions = appointment.Prescription.Instructions
                });
            }

            return View(new PrescriptionViewModel { AppointmentId = appointmentId });
        }

        [HttpPost]
        public async Task<IActionResult> Prescription(PrescriptionViewModel model)
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var appt = await _context.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p!.User)
                    .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);
                ViewBag.Appointment = appt;
                return View(model);
            }

            var existing = await _context.Prescriptions.FirstOrDefaultAsync(p => p.AppointmentId == model.AppointmentId);
            if (existing != null)
            {
                existing.Medication = model.Medication;
                existing.Dosage = model.Dosage;
                existing.Instructions = model.Instructions;
            }
            else
            {
                _context.Prescriptions.Add(new Prescription
                {
                    AppointmentId = model.AppointmentId,
                    Medication = model.Medication,
                    Dosage = model.Dosage,
                    Instructions = model.Instructions,
                    PrescribedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Prescription saved successfully.";
            return RedirectToAction("Appointments");
        }
    }
}
