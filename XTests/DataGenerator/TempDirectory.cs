namespace XTests.DataGenerator;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("trading-mcp-tests-").FullName;

    public string CreateSubdirectory(string name) => Directory.CreateDirectory(System.IO.Path.Combine(Path, name)).FullName;

    public void WriteEnvFile(string contents) => File.WriteAllText(System.IO.Path.Combine(Path, ".env"), contents);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
