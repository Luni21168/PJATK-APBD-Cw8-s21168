namespace HospitalApi.DTOs;

public class BedAssignmentResponseDto
{
    public int Id { get; set; }
    public string PatientPesel { get; set; } = string.Empty;
    public int BedId { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}