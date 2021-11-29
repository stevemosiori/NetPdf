// See https://aka.ms/new-console-template for more information
using System.Reflection;
using Tephanik;

NetPdf pdf = new();

pdf.AddPage("P", "A4");

pdf.SetFont("Helvetica", "I", 50);
pdf.SetTextColor(0, 0, 0);
pdf.Cell(28, 4.5, "Hello World", 0, 0, "L", false);
pdf.Output("test.pdf", "F");

// Dictionary<string, dynamic>[] d = new Dictionary<string, dynamic>[2];
// d["age"] = 67;
// d["age"] = 56;

// Console.WriteLine($"{12:X3} G");