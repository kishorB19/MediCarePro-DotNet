using HMS.Data;
using HMS.Models;
using HMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMS.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Patient?> GetCurrentPatient()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        public async Task<IActionResult> Dashboard()
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                TotalAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patient.PatientId),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patient.PatientId && a.Status == AppointmentStatus.Pending),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patient.PatientId && a.Status == AppointmentStatus.Completed),
                TodayAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patient.PatientId && a.AppointmentDate.Date == DateTime.Today),
                RecentAppointments = await _context.Appointments
                    .Include(a => a.Doctor).ThenInclude(d => d!.User)
                    .Where(a => a.PatientId == patient.PatientId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(5)
                    .ToListAsync()
            };

            ViewBag.PatientName = patient.User?.FullName;
            return View(model);
        }

        public async Task<IActionResult> Doctors(string? search)
        {
            var query = _context.Doctors.Include(d => d.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => d.Specialization.Contains(search) || d.User!.FullName.Contains(search));
            }
            var doctors = await query.ToListAsync();
            ViewBag.Search = search;
            return View(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> BookAppointment(int doctorId)
        {
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == doctorId);
            if (doctor == null) return NotFound();

            var model = new BookAppointmentViewModel
            {
                DoctorId = doctorId,
                Doctor = doctor,
                AppointmentDate = DateTime.Today.AddDays(1)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                model.Doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == model.DoctorId);
                return View(model);
            }

            var exists = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == model.DoctorId &&
                a.AppointmentDate.Date == model.AppointmentDate.Date &&
                a.TimeSlot == model.TimeSlot &&
                a.Status != AppointmentStatus.Cancelled);

            if (exists)
            {
                ModelState.AddModelError("", "This time slot is already booked.");
                model.Doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == model.DoctorId);
                return View(model);
            }

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.DoctorId,
                AppointmentDate = model.AppointmentDate,
                TimeSlot = model.TimeSlot,
                Reason = model.Reason,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> Appointments()
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var appointments = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.User)
                .Include(a => a.Prescription)
                .Where(a => a.PatientId == patient.PatientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id && a.PatientId == patient.PatientId);
            if (appointment != null && appointment.Status == AppointmentStatus.Pending)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment cancelled.";
            }
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> Prescriptions()
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.User)
                .Where(p => p.Appointment!.PatientId == patient.PatientId)
                .OrderByDescending(p => p.PrescribedDate)
                .ToListAsync();

            return View(prescriptions);
        }
    }
}
