namespace Aurora.Flowboard.Domain.Users;

public sealed class Role
{
    public static readonly Role Administrator = new("Administrator");
    public static readonly Role Member = new("Member");

    public static readonly IReadOnlyCollection<Role> All = [Administrator, Member];

    public string Name { get; private set; }

    private Role(string name)
    {
        Name = name;
    }

    private Role() { }

    public static Result<Role> FromName(string name)
    {
        Role? role = All.FirstOrDefault(r => r.Name == name);

        return role is null
            ? Result.Fail<Role>(RoleErrors.NotFound)
            : role;
    }
}
