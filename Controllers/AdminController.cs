using HMS.Data;
using HMS.Models;
using HMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending),
                TodayAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed),
                RecentAppointments = await _context.Appointments
                    .Include(a => a.Doctor).ThenInclude(d => d!.User)
                    .Include(a => a.Patient).ThenInclude(p => p!.User)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .ToListAsync()
            };
            return View(model);
        }

        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors.Include(d => d.User).ToListAsync();
            return View(doctors);
        }

        [HttpGet]
        public IActionResult CreateDoctor() => View();

        [HttpPost]
        public async Task<IActionResult> CreateDoctor(CreateDoctorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = "Doctor",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Doctor");

                var doctor = new Doctor
                {
                    UserId = user.Id,
                    Specialization = model.Specialization,
                    Qualification = model.Qualification,
                    ExperienceYears = model.ExperienceYears,
                    ConsultationFee = model.ConsultationFee,
                    Bio = model.Bio
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Doctor created successfully.";
                return RedirectToAction("Doctors");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == id);
            if (doctor != null)
            {
                if (doctor.User != null)
                    await _userManager.DeleteAsync(doctor.User);
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Doctor removed successfully.";
            }
            return RedirectToAction("Doctors");
        }

        public async Task<IActionResult> Patients()
        {
            var patients = await _context.Patients.Include(p => p.User).ToListAsync();
            return View(patients);
        }

        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
            return View(appointments);
        }
    }
}
