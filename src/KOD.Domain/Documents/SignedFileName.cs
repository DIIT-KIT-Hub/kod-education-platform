namespace KOD.Domain.Documents;

/// <summary>
/// Names the file of a signature after the signed document, so a download keeps the name the author uploaded:
/// a signed PDF (PAdES) is the document itself and keeps its name; containers add their own extension
/// (<c>contract.pdf.p7s</c>, <c>contract.pdf.asics</c>).
/// </summary>
public static class SignedFileName
{
    /// <summary>
    /// Returns the file name of a signature of the document.
    /// </summary>
    /// <param name="documentFileName">The file name of the signed document.</param>
    /// <param name="format">The signature format.</param>
    /// <returns>The file name.</returns>
    public static string For(string documentFileName, SignatureFormat format)
    {
        ArgumentNullException.ThrowIfNull(documentFileName);
        var name = Path.GetFileName(documentFileName.Trim());
        if (name.Length == 0)
        {
            name = "document";
        }

        var result = format switch
        {
            SignatureFormat.PAdES => Path.ChangeExtension(name, ".pdf"),
            SignatureFormat.XAdES => $"{name}.xml",
            SignatureFormat.AsicS => $"{name}.asics",
            SignatureFormat.AsicE => $"{Path.GetFileNameWithoutExtension(name)}.asice",
            _ => $"{name}.p7s",
        };

        // Keep the extension when a very long name has to be shortened.
        return result.Length <= StoredFile.FileNameMaxLength
            ? result
            : string.Concat(Path.GetFileNameWithoutExtension(result).AsSpan(0, StoredFile.FileNameMaxLength - Path.GetExtension(result).Length), Path.GetExtension(result));
    }
}
