# NetPdf
PDF Generator for .NET

```csharp
NetPdf pdf = new();

pdf.AddPage("P", "A4");

pdf.SetFont("Helvetica", "I", 10);
pdf.SetTextColor(0, 0, 0);
pdf.Cell(28, 4.5, "Hello World!", 0, 0, "L", false);
pdf.Output("doc.pdf", "F");
```
