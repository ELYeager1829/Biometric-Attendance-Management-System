using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BiometricClockingSystem.Api.Controllers;

// Handles the "Call Administrator" flow from the kiosk: the kiosk raises a
// request via /notify, and an admin resolves it via /resolve. The frontend
// gates this with a client-side Windows Hello/WebAuthn prompt (same pattern
// already used by Dashboard.jsx's scanFingerprint/DeleteModal) before ever
// calling this endpoint - this endpoint trusts that check already happened
// and just records which admin approved it, matching that existing model.
[ApiController]
[Route("api/admin")]
public class AdminOverrideController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendanceService _attendanceService;

    public AdminOverrideController(
        ApplicationDbContext context,
        IAttendanceService attendanceService)
    {
        _context = context;
        _attendanceService = attendanceService;
    }

    // Matches: fetch('/api/admin/notify', { body: { employeeNumber, reason } })
    [HttpPost("notify")]
    public async Task<IActionResult> Notify([FromBody] NotifyAdminRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeNumber == request.EmployeeNumber && e.IsActive);

        if (employee == null)
            return NotFound(new { success = false, message = "Employee not found." });

        var overrideRequest = new OverrideRequest
        {
            EmployeeId = employee.EmployeeNumber,
            RequestedClockType = ClockType.ClockIn,
            RequestedAt = DateTime.UtcNow,
            Status = OverrideRequestStatus.Pending,
        };

        _context.OverrideRequests.Add(overrideRequest);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, overrideRequestId = overrideRequest.OverrideRequestId });
    }

    // For the admin dashboard: everyone currently waiting for approval.
    [HttpGet("override-requests")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var pending = await _context.OverrideRequests
            .Where(r => r.Status == OverrideRequestStatus.Pending)
            .Include(r => r.Employee)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

        return Ok(pending);
    }

    // Called after the frontend's Windows Hello/WebAuthn prompt succeeds.
    [HttpPost("override-requests/{id}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveOverrideRequest request)
    {
        var overrideRequest = await _context.OverrideRequests
            .FirstOrDefaultAsync(r => r.OverrideRequestId == id);

        if (overrideRequest == null || overrideRequest.Status != OverrideRequestStatus.Pending)
            return NotFound(new { success = false, message = "No pending request found." });

        var admin = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.AdminId && u.IsActive);

        if (admin == null)
            return BadRequest(new { success = false, message = "Admin not found." });

        overrideRequest.Status = OverrideRequestStatus.Resolved;
        overrideRequest.ResolvedByAdminUsername = admin.Email;
        overrideRequest.ResolvedAt = DateTime.UtcNow;

        var attendance = overrideRequest.RequestedClockType == ClockType.ClockIn
            ? await _attendanceService.ClockInAsync(overrideRequest.EmployeeId, ClockAuthMethod.FingerprintOverride, admin.Email)
            : await _attendanceService.ClockOutAsync(overrideRequest.EmployeeId, ClockAuthMethod.FingerprintOverride, admin.Email);

        _context.AdminOverrides.Add(new AdminOverride
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = overrideRequest.EmployeeId,
            AdminId = admin.Id,
            CreatedAt = DateTime.UtcNow,
            Successful = true
        });

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Override approved.", attendanceId = attendance?.AttendanceId });
    }

    public sealed class NotifyAdminRequest
    {
        public string EmployeeNumber { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }

    public sealed class ResolveOverrideRequest
    {
        public Guid AdminId { get; init; }
    }
}
