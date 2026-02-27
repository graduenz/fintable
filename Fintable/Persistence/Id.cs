namespace Fintable.Persistence;

public static class Id
{
    public static string New() => Ulid.NewUlid().ToString();
}
