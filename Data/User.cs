namespace LearnToCode.Data;

public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Lietotājvārds — jau normalizēts (lowercase, trim) pirms saglabāšanas DB.
    /// Unikalitāte tiek nodrošināta ar LOWER() funkcijas indeksu.
    /// </summary>
    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// Atvasināts no FirstName + LastName — netiek glabāts datu bāzē.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// E-pasts — jau normalizēts (lowercase, trim) pirms saglabāšanas DB.
    /// Unikalitāte tiek nodrošināta ar LOWER() funkcijas indeksu.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Pārstāvniecība (skola, uzņēmums, organizācija). Iepriekšējais nosaukums: EducationInstitution.
    /// </summary>
    public string? Representation { get; set; }

    /// <summary>
    /// Lietotāja īss apraksts par sevi (max 500 rakstzīmes).
    /// </summary>
    public string? Bio { get; set; }

    public int Rating { get; set; } = 1000;

    public UserRole Role { get; set; } = UserRole.Audzeknis;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
