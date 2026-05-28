using HospitalApi.Data;
using HospitalApi.DTOs;
using HospitalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PatientsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientGetDto>>> GetPatients([FromQuery] string? search)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();

            query = query.Where(p =>
                p.FirstName.ToLower().Contains(searchLower) ||
                p.LastName.ToLower().Contains(searchLower) ||
                p.Pesel.ToLower().Contains(searchLower));
        }

        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PatientGetDto
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpPost("{pesel}/bedassignments")]
    public async Task<ActionResult<BedAssignmentResponseDto>> CreateBedAssignment(
        [FromRoute] string pesel,
        [FromBody] CreateBedAssignmentDto request)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Pesel == pesel);

        if (patient == null)
        {
            return NotFound(new { message = $"Patient with PESEL {pesel} was not found." });
        }

        var bed = await _context.Beds
            .FirstOrDefaultAsync(b => b.Id == request.BedId);

        if (bed == null)
        {
            return NotFound(new { message = $"Bed with id {request.BedId} was not found." });
        }

        if (request.To.HasValue && request.To.Value <= request.From)
        {
            return BadRequest(new { message = "To must be greater than From." });
        }

        var overlappingAssignment = await _context.BedAssignments
            .AnyAsync(ba =>
                ba.BedId == request.BedId &&
                request.From < (ba.To ?? DateTime.MaxValue) &&
                ba.From < (request.To ?? DateTime.MaxValue));

        if (overlappingAssignment)
        {
            return Conflict(new { message = "This bed is already assigned in the given period." });
        }

        var bedAssignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = request.BedId,
            From = request.From,
            To = request.To
        };

        _context.BedAssignments.Add(bedAssignment);
        await _context.SaveChangesAsync();

        var response = new BedAssignmentResponseDto
        {
            Id = bedAssignment.Id,
            PatientPesel = bedAssignment.PatientPesel,
            BedId = bedAssignment.BedId,
            From = bedAssignment.From,
            To = bedAssignment.To
        };

        return Created($"/api/patients/{pesel}/bedassignments/{bedAssignment.Id}", response);
    }
}