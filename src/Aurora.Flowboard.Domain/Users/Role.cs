namespace Aurora.Flowboard.Domain.Users;

public sealed class Role
{
    public static readonly Role Administrator = new("Administrator");
    public static readonly Role Member = new("Member");

    public string Name { get; private set; }

    private Role(string name)
    {
        Name = name;
    }

    private Role() { }
}
