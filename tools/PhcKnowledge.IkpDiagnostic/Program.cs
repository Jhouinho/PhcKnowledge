using System.Security.Cryptography;
using System.Text.Json;
using PhcKnowledge.Import.Validation;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("Utilização:");
    Console.WriteLine("  dotnet run --project tools/PhcKnowledge.IkpDiagnostic -- <caminho-do-pacote.zip> [--report <caminho-do-relatorio.json>]");
    return args.Length == 0 ? 64 : 0;
}

var packagePath = Path.GetFullPath(args[0]);
string? reportPath = null;
for (var index = 1; index < args.Length; index++)
{
    if (args[index] == "--report" && index + 1 < args.Length)
    {
        reportPath = Path.GetFullPath(args[++index]);
        continue;
    }

    Console.Error.WriteLine($"Argumento desconhecido ou incompleto: {args[index]}");
    return 64;
}

if (!File.Exists(packagePath))
{
    Console.Error.WriteLine($"Ficheiro não encontrado: {packagePath}");
    return 2;
}

try
{
    var fileInfo = new FileInfo(packagePath);
    await using var packageStream = new FileStream(
        packagePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 1024 * 1024,
        options: FileOptions.Asynchronous | FileOptions.SequentialScan);

    var packageSha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(packageStream));
    packageStream.Position = 0;

    var validator = new IkpPackageValidator();
    var validation = await validator.ValidateAsync(packageStream);

    var report = new
    {
        diagnosticVersion = "1.0.0",
        checkedAtUtc = DateTimeOffset.UtcNow,
        package = new
        {
            fileName = fileInfo.Name,
            byteLength = fileInfo.Length,
            sha256 = packageSha256
        },
        validation = new
        {
            isValid = validation.IsValid,
            packageId = validation.PackageId,
            captureId = validation.CaptureId,
            fileCount = validation.FileCount,
            artifactCount = validation.ArtifactCount,
            objectCount = validation.ObjectCount,
            operationCount = validation.OperationCount,
            errors = validation.Errors
        }
    };

    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    Console.WriteLine(json);

    if (!string.IsNullOrWhiteSpace(reportPath))
    {
        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(reportPath, json);
        Console.Error.WriteLine($"Relatório gravado em: {reportPath}");
    }

    return validation.IsValid ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Falha ao executar diagnóstico: {exception.GetType().Name}: {exception.Message}");
    return 2;
}
