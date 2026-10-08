namespace FixtureHub.ArchitectureTests;

internal static class Repository
{
    public static string File(string relativePath)
    {
        var path = Path.Combine(Root(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(System.IO.File.Exists(path), $"No existe el archivo {path}");

        return path;
    }

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !System.IO.File.Exists(Path.Combine(directory.FullName, "FixtureHub.slnx")))
            directory = directory.Parent;

        Assert.NotNull(directory);

        return directory.FullName;
    }
}
