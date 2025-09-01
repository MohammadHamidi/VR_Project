public class ValidationResult
{
    public bool IsValid { get; set; }
    public string[] Errors { get; set; } = new string[0];
    public string[] Warnings { get; set; } = new string[0];
}