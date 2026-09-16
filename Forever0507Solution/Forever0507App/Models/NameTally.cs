namespace Forever0507App.Models;

/// <summary>Registrations grouped by a label (district or school) for the dashboard tallies.</summary>
public class NameTally
{
    public string Name { get; set; } = "";
    public int Total { get; set; }
    public int Approved { get; set; }
}
