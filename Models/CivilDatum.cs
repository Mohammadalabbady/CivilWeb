using System;
using System.Collections.Generic;

namespace CivilWeb.Models;

public partial class CivilDatum
{
    public string Id { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? SecondName { get; set; }

    public string? ThirdName { get; set; }

    public string? LastName { get; set; }

    public string? FullName { get; set; }

    public string? RegistrationNumber { get; set; }

    public DateOnly? BirhtOfDate { get; set; }

    public string? Gender { get; set; }

    public bool? Alive { get; set; }
}
