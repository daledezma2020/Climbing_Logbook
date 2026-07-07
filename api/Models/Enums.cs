namespace api.Models;

public enum ClimbDiscipline
{
    Bouldering = 0,
    Sport = 1,
    Trad = 2,
    Other = 99
}

public enum GradeSystem
{
    VScale = 0,
    Yds = 1,
    French = 2,
    Font = 3,
    Other = 99
}

public enum PlaceKind
{
    Gym = 0,
    Outdoor = 1,
    Custom = 2
}

public enum LogEntryStatus
{
    Attempted = 0,
    Completed = 1
}

public enum ExternalProvider
{
    Local = 0,
    OpenBeta = 1,
    OpenStreetMap = 2,
    MoonBoardSeed = 3
}
