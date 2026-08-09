namespace AstraSystemsRental.Users.Api.Domain;

public static class PersonType
{
    public const string Natural = "Natural";
    public const string Juridico = "Juridico";

    public static bool IsValid(string? value) =>
        value is Natural or Juridico;
}

public static class RoleCode
{
    public const string Standard = "Standard";
    public const string Demo = "Demo";
    public const string SuperUser = "SuperUser";
    public const string SysAdmin = "SysAdmin";
}

public static class PlanCode
{
    public const string Demo = "Demo";
    public const string Basic = "Basic";
}
