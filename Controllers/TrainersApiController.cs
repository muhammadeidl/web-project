using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers.Api
{
    [ApiController]
    [Route("api/trainers")]
    public class TrainersApiController : ControllerBase
    {
        private readonly SporSalonuDbContext _context;

        public TrainersApiController(SporSalonuDbContext context)
        {
            _context = context;
        }

        // GET: /api/trainers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Coaches
                .Include(c => c.Gym)
                .Select(c => new
                {
                    id = c.CoachId,
                    fullName = c.Name,
                    specialty = c.Expertise,
                    fitnessCenterName = c.Gym != null ? c.Gym.Name : "-"
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: /api/trainers/available?date=2025-12-15&minMinutes=30
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable(
            [FromQuery] DateTime date,
            [FromQuery] int minMinutes = 30)
        {
            var day = (GymDay)date.DayOfWeek;
            var start = date.Date;
            var end = start.AddDays(1);

            var coaches = await _context.Coaches
                .Include(c => c.Gym)
                .Include(c => c.CoachAvailabilities)
                .Include(c => c.UnavailableSlots.Where(u => u.StartTime < end && u.EndTime > start))
                .Include(c => c.Appointments.Where(a =>
                        a.AppointmentDate >= start &&
                        a.AppointmentDate < end &&
                        (a.Status == AppointmentStatus.Pending ||
                         a.Status == AppointmentStatus.Confirmed)))
                    .ThenInclude(a => a.TrainingProgram)
                .Where(c => c.CoachAvailabilities.Any(a =>
                    a.DayOfWeek == day &&
                    !a.IsClosed &&
                    a.StartTime.HasValue &&
                    a.EndTime.HasValue))
                .ToListAsync();

            var result = new List<object>();
            var needed = TimeSpan.FromMinutes(minMinutes);

            foreach (var coach in coaches)
            {
                var availability = coach.CoachAvailabilities
                    .First(a => a.DayOfWeek == day);

                // ✅ حوّلهم إلى TimeSpan عادي
                var availStart = availability.StartTime!.Value;
                var availEnd = availability.EndTime!.Value;

                // Busy intervals
                var busy = new List<(TimeSpan s, TimeSpan e)>();

                // Appointments
                foreach (var appt in coach.Appointments)
                {
                    var dur = appt.TrainingProgram?.Duration ?? 0;
                    var s = appt.AppointmentDate.TimeOfDay;
                    var e = s.Add(TimeSpan.FromMinutes(dur));
                    busy.Add((s, e));
                }

                // Unavailable slots
                foreach (var u in coach.UnavailableSlots)
                {
                    var s = u.StartTime < start
                        ? TimeSpan.Zero
                        : u.StartTime.TimeOfDay;

                    var e = u.EndTime > end
                        ? new TimeSpan(23, 59, 59)
                        : u.EndTime.TimeOfDay;

                    busy.Add((s, e));
                }

                // ✂️ Clip busy intervals to availability
                busy = busy
                    .Select(x => (
                        s: x.s < availStart ? availStart : x.s,
                        e: x.e > availEnd ? availEnd : x.e))
                    .Where(x => x.s < x.e)
                    .OrderBy(x => x.s)
                    .ToList();

                // Merge busy intervals
                var merged = new List<(TimeSpan s, TimeSpan e)>();
                foreach (var b in busy)
                {
                    if (merged.Count == 0)
                    {
                        merged.Add(b);
                    }
                    else
                    {
                        var last = merged[^1];
                        if (b.s <= last.e)
                        {
                            merged[^1] = (last.s, b.e > last.e ? b.e : last.e);
                        }
                        else
                        {
                            merged.Add(b);
                        }
                    }
                }

                // Gap check
                bool hasGap = false;

                if (merged.Count == 0)
                {
                    hasGap = (availEnd - availStart) >= needed;
                }
                else
                {
                    // before first
                    if ((merged[0].s - availStart) >= needed)
                        hasGap = true;

                    // between
                    for (int i = 0; i < merged.Count - 1 && !hasGap; i++)
                    {
                        if ((merged[i + 1].s - merged[i].e) >= needed)
                            hasGap = true;
                    }

                    // after last
                    if (!hasGap && (availEnd - merged[^1].e) >= needed)
                        hasGap = true;
                }

                if (hasGap)
                {
                    result.Add(new
                    {
                        id = coach.CoachId,
                        fullName = coach.Name,
                        specialty = coach.Expertise,
                        fitnessCenterName = coach.Gym != null ? coach.Gym.Name : "-"
                    });
                }
            }

            return Ok(result);
        }
    }
}
