namespace LearningSystemAPI.DTOs;

public class SubjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public class PresentationDto
{
    public Guid Id { get; set; }
    public string Label { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; }
}

