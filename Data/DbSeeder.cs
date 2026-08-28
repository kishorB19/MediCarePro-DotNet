using HMS.Models;
using Microsoft.AspNetCore.Identity;

namespace HMS.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roles = { "Admin", "Doctor", "Patient" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var adminEmail = "admin@hms.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    Role = "Admin",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            var doctorData = new[]
            {
                new { Email = "dr.sharma@hms.com", Name = "Dr. Rajesh Sharma", Spec = "Cardiology", Qual = "MBBS, MD (Cardiology)", Exp = 15, Fee = 800m, Bio = "Senior Cardiologist with 15 years of experience in interventional cardiology and heart disease management." },
                new { Email = "dr.patel@hms.com", Name = "Dr. Priya Patel", Spec = "Dermatology", Qual = "MBBS, MD (Dermatology)", Exp = 10, Fee = 600m, Bio = "Expert dermatologist specializing in skin disorders, cosmetic dermatology, and laser treatments." },
                new { Email = "dr.khan@hms.com", Name = "Dr. Amir Khan", Spec = "Orthopedics", Qual = "MBBS, MS (Orthopedics)", Exp = 12, Fee = 700m, Bio = "Orthopedic surgeon with expertise in joint replacement, sports injuries, and spine surgery." },
                new { Email = "dr.gupta@hms.com", Name = "Dr. Sneha Gupta", Spec = "Pediatrics", Qual = "MBBS, MD (Pediatrics)", Exp = 8, Fee = 500m, Bio = "Compassionate pediatrician dedicated to child healthcare, vaccinations, and developmental assessments." }
            };

            foreach (var d in doctorData)
            {
                if (await userManager.FindByEmailAsync(d.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = d.Email,
                        Email = d.Email,
                        FullName = d.Name,
                        Role = "Doctor",
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user, "Doctor@123");
                    await userManager.AddToRoleAsync(user, "Doctor");

                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        Specialization = d.Spec,
                        Qualification = d.Qual,
                        ExperienceYears = d.Exp,
                        ConsultationFee = d.Fee,
                        Bio = d.Bio
                    };
                    context.Doctors.Add(doctor);
                }
            }
            await context.SaveChangesAsync();

            var patientData = new[]
            {
                new { Email = "rahul@gmail.com", Name = "Rahul Verma", Dob = new DateTime(1990, 5, 15), Gender = "Male", Blood = "O+", Address = "123, MG Road, Mumbai", Phone = "9876543210" },
                new { Email = "anita@gmail.com", Name = "Anita Singh", Dob = new DateTime(1985, 8, 22), Gender = "Female", Blood = "A+", Address = "456, Park Street, Delhi", Phone = "9876543211" }
            };

            foreach (var p in patientData)
            {
                if (await userManager.FindByEmailAsync(p.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = p.Email,
                        Email = p.Email,
                        FullName = p.Name,
                        Role = "Patient",
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user, "Patient@123");
                    await userManager.AddToRoleAsync(user, "Patient");

                    var patient = new Patient
                    {
                        UserId = user.Id,
                        DateOfBirth = p.Dob,
                        Gender = p.Gender,
                        BloodGroup = p.Blood,
                        Address = p.Address,
                        Phone = p.Phone
                    };
                    context.Patients.Add(patient);
                }
            }
            await context.SaveChangesAsync();

            var doctors = context.Doctors.ToList();
            if (!context.Availabilities.Any())
            {
                var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
                foreach (var doc in doctors)
                {
                    foreach (var day in days)
                    {
                        context.Availabilities.Add(new Availability
                        {
                            DoctorId = doc.DoctorId,
                            DayOfWeek = day,
                            StartTime = new TimeSpan(9, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            IsActive = true
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

            if (!context.Appointments.Any())
            {
                var patients = context.Patients.ToList();
                if (patients.Count >= 2 && doctors.Count >= 2)
                {
                    context.Appointments.Add(new Appointment
                    {
                        PatientId = patients[0].PatientId,
                        DoctorId = doctors[0].DoctorId,
                        AppointmentDate = DateTime.Today.AddDays(1),
                        TimeSlot = "10:00 AM",
                        Status = AppointmentStatus.Confirmed,
                        Reason = "Routine heart checkup",
                        CreatedAt = DateTime.UtcNow
                    });
                    context.Appointments.Add(new Appointment
                    {
                        PatientId = patients[1].PatientId,
                        DoctorId = doctors[1].DoctorId,
                        AppointmentDate = DateTime.Today.AddDays(2),
                        TimeSlot = "11:00 AM",
                        Status = AppointmentStatus.Pending,
                        Reason = "Skin rash consultation",
                        CreatedAt = DateTime.UtcNow
                    });
                    context.Appointments.Add(new Appointment
                    {
                        PatientId = patients[0].PatientId,
                        DoctorId = doctors[2].DoctorId,
                        AppointmentDate = DateTime.Today.AddDays(-5),
                        TimeSlot = "02:00 PM",
                        Status = AppointmentStatus.Completed,
                        Reason = "Knee pain follow-up",
                        CreatedAt = DateTime.UtcNow.AddDays(-6)
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
