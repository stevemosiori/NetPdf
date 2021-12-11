namespace Tephanik
{
    public class Font
    {
        public string? fontkey { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public double up { get; set; }
        public double ut { get; set; }
        public List<(char, int)> cw { get; set; }
        public string enc { get; set; }
        public List<(int, dynamic)> uv { get; set; }
        public double? i { get; set; }
        public double? n { get; set; }
    }
}