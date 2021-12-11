using System.Collections;
using System.Reflection;
using System.Text;

namespace Tephanik.NetPdf
{
    public class NetPdf
    {
        private int CurrentPageNo;                // current page number
        private int CurrentObjectNo;              // current object number
        private Dictionary<int, int> offsets;     // array of object offsets
        private StringBuilder buffer;             // buffer holding in-memory PDF
        private Dictionary<int, string> pages;    // array containing pages
        // private List<string> pages;              // array containing pages
        private int state;                        // current document state
        private bool compress;                    // compression flag
        private double ScaleFactor;               // scale factor (number of points in user unit)
        private string DefOrientation;            // default orientation
        private string CurOrientation;            // current orientation
        private Dictionary<string, PageSize> StdPageSizes;             // standard page sizes
        private PageSize DefPageSize;             // default page size
        private PageSize CurPageSize;             // current page size
        private double CurRotation;               // current page rotation
        private Dictionary<string, dynamic>[] PageInfo;           // page-related data
        private double wPt, hPt;                  // dimensions of current page in points
        private double CurrentPageHeight;         // dimensions of current page in user unit
        private double CurrentPageWidth;          // dimensions of current page in user unit
        private double LeftMargin;                // left margin
        private double TopMargin;                 // top margin
        private double RightMargin;               // right margin
        private double BottomMargin;              // page break margin
        private double CellMargin;                // cell margin
        private double CurrentX;                  // current position in user unit
        private double CurrentY;                  // current position in user unit
        private double lasth;                     // height of last printed cell
        private double LineWidth;                 // line width in user unit
        private List<string> CoreFonts;           // array of core font names
        private Dictionary<string, Font> fonts;   // array of used fonts
        private object? FontFiles;                // array of font files
        private object? encodings;                // array of encodings
        private Dictionary<string, int> cmaps;    // array of ToUnicode CMaps
        private string FontFamily;                // current font family
        private string FontStyle;                 // current font style
        private bool underline;                   // underlining flag
        private Font CurrentFont;                 // current font info
        private double FontSizePt;                // current font size in points
        private double FontSize;                  // current font size in user unit
        private string DrawColor;                 // commands for drawing color
        private string FillColor;                 // commands for filling color
        private string TextColor;                 // commands for text color
        private bool ColorFlag;                   // indicates whether fill and text colors are different
        private bool WithAlpha;                   // indicates whether alpha channel is used
        private double WordSpacing;               // word spacing
        private List<object>? images;             // array of used images
        private List<object>? PageLinks;          // array of links in pages
        private List<object>? links;              // array of internal links
        private bool AutoPageBreak;               // automatic page breaking
        private double PageBreakTrigger;          // threshold used to trigger page breaks
        private bool InHeader;                    // flag set when processing header
        private bool InFooter;                    // flag set when processing footer
        private string? AliasNbPages;             // alias for total number of pages
        private string? ZoomMode;                 // zoom display mode
        private string? LayoutMode;               // layout display mode
        private Dictionary<string, string> metadata;           // document properties
        private string PDFVersion;                // PDF version number
        private string NetPdfVersion = "1.0";

        public NetPdf(
            string orientation = "P",
            string unit = "mm",
            string inputSize = "A4")
        {
            // Some checks
            // this._dochecks();
            // Initialization of properties
            this.state = 0;
            this.CurrentPageNo = 0;
            this.CurrentObjectNo = 2;
            this.buffer = new StringBuilder();
            this.pages = new();
            this.PageInfo = new Dictionary<string, dynamic>[1000];
            for (var i = 0; i < this.PageInfo.Count(); i++)
            {
                this.PageInfo[i] = new();
            }
            this.fonts = new();
            this.FontFiles = new();
            this.encodings = new();
            this.cmaps = new();
            this.images = new();
            this.links = new();
            this.offsets = new();
            this.InHeader = false;
            this.InFooter = false;
            this.lasth = 0.0;
            this.FontFamily = "";
            this.FontStyle = "";
            this.FontSizePt = 12.0;
            this.underline = false;
            this.DrawColor = "0 G";
            this.FillColor = "0 g";
            this.TextColor = "0 g";
            this.ColorFlag = false;
            this.WithAlpha = false;
            this.WordSpacing = 0;
            
            // Core fonts
            this.CoreFonts = new List<string>{
                "courier", "helvetica", "times", "symbol", "zapfdingbats"
            };

            // Scale factor
            if(unit.Equals("pt")) {
                this.ScaleFactor = 1;
            } else if(unit.Equals("mm")) {
                this.ScaleFactor = 72/25.4;
            } else if(unit.Equals("cm")) {
                this.ScaleFactor = 72/2.54;
            } else if(unit.Equals("in")) {
                this.ScaleFactor = 72;
            } else {
                this.Error($"Incorrect unit: {unit.ToString()}");
            }

            // Page sizes
            this.StdPageSizes = new Dictionary<string, PageSize>{
                {"a3", new PageSize {Width = 841.89, Height = 1190.55}},
                {"a4", new PageSize {Width = 595.28, Height = 841.89}},
                {"a5", new PageSize {Width = 419.53, Height = 595.28}},
                {"letter", new PageSize {Width = 612.0, Height = 792.0}},
                {"legal", new PageSize {Width = 612.0, Height = 1008.0}}
            };

            var size = this.GetPageSize(inputSize);
            this.DefPageSize = size;
            this.CurPageSize = size;
            // Page orientation
            orientation = orientation.ToLower();
            this.DefOrientation = "";
            if(orientation.Equals("p") || orientation.Equals("portrait")) {
                this.DefOrientation = "P";
                this.CurrentPageWidth = size.Width;
                this.CurrentPageHeight = size.Height;
            } else if(orientation.Equals("l") || orientation.Equals("landscape")) {
                this.DefOrientation = "L";
                this.CurrentPageWidth = size.Height;
                this.CurrentPageHeight = size.Width;
            } else {
                this.Error($"Incorrect orientation: {orientation.ToString()}");
            }

            this.CurOrientation = this.DefOrientation;
            this.wPt = this.CurrentPageWidth*this.ScaleFactor;
            this.hPt = this.CurrentPageHeight*this.ScaleFactor;
            // Page rotation
            this.CurRotation = 0.0;
            // Page margins (1 cm)
            double margin = 28.35/this.ScaleFactor;
            this.SetMargins(margin, margin);
            // Interior cell margin (1 mm)
            this.CellMargin = margin/10;
            // Line width (0.2 mm)
            this.LineWidth = .567/this.ScaleFactor;
            // Automatic page break
            this.SetAutoPageBreak(true,2*margin);
            // Default display mode
            this.SetDisplayMode("default");
            // Enable compression
            this.SetCompression(false);
            // Set default PDF version number
            this.PDFVersion = "1.3";

            this.metadata = new();

            this.CurrentFont = Fonts.Times;
            // TODO: fix
            // this.AddFont("Helvetica", "I");
        }

        private PageSize GetPageSize(dynamic size)
        {
            if(size is string)
            {
                size = size.ToLower();
                if(! this.StdPageSizes.ContainsKey(size)) {
                    this.Error("Unknown page size: " + size.ToString());
                }
                return this.StdPageSizes[size];
            } else {
                if(size.Width > size.Height)
                    return new PageSize { Width = size.Height, Height = size.Width };
                else
                    return new PageSize { Width = size.Width, Height = size.Height };
            }
        }

        private void Error(string msg) => throw new Exception("NetPdf error: " + msg);

        public void Ln(double? h = null)
        {
            // Line feed; default value is the last cell height
            this.CurrentX = this.LeftMargin;
            if(h == null)
                this.CurrentY += this.lasth;
            else
                this.CurrentY += (double) h;
        }

        public void SetMargins(double left, double top, double? right = null)
        {
            // Set left, top and right margins
            this.LeftMargin = left;
            this.TopMargin = top;
            
            if(right == null) {
                right = left;
            }

            this.RightMargin = (double) right;
        }

        public void SetLeftMargin(double margin)
        {
            // Set left margin
            this.LeftMargin = margin;
            if(this.CurrentPageNo > 0 && this.CurrentX < margin) {
                this.CurrentX = margin;
            }
        }

        public void SetTopMargin(double margin)
        {
            // Set top margin
            this.TopMargin = margin;
        }

        public void SetRightMargin(double margin)
        {
            // Set right margin
            this.RightMargin = margin;
        }

        public void SetAutoPageBreak(bool auto, double margin = 0.0)
        {
            // Set auto page break mode and triggering margin
            this.AutoPageBreak = auto;
            this.BottomMargin = margin;
            this.PageBreakTrigger = this.CurrentPageHeight - margin;
        }

        public void SetDisplayMode(string zoom, string layout = "default")
        {
            // Set display mode in viewer
            if(zoom.Equals("fullpage")
               || zoom.Equals("fullwidth")
               || zoom.Equals("real")
               || zoom.Equals("default")
               || zoom is string) {
                this.ZoomMode = zoom;
            }  else {
                this.Error($"Incorrect zoom display mode: {zoom.ToString()}");
            }

            if(layout.Equals("single")
               || layout.Equals("continuous")
               || layout.Equals("two")
               || layout.Equals("default"))
                this.LayoutMode = layout;
            else {
                this.Error($"Incorrect layout display mode: {layout.ToString()}");
            }
        }

        public void SetCompression(bool compress)
        {
            // TODO: fix
            // Set page compression
            // if(function_exists("gzcompress"))
            //     this.compress = $compress;
            // else
            //     this.compress = false;
            this.compress = false;
        }

        public void SetTitle(string title, bool isUTF8 = false)
        {
            // Title of document
            this.metadata["Title"] = isUTF8 ? title : UTF8_encode(title);
        }

        public void SetAuthor(string author, bool isUTF8=false)
        {
            // Author of document
            this.metadata["Author"] = isUTF8 ? author : UTF8_encode(author);
        }

        public void SetSubject(string subject, bool isUTF8=false)
        {
            // Subject of document
            this.metadata["Subject"] = isUTF8 ? subject : UTF8_encode(subject);
        }

        public void SetKeywords(string keywords, bool isUTF8=false)
        {
            // Keywords of document
            this.metadata["Keywords"] = isUTF8 ? keywords : UTF8_encode(keywords);
        }

        public void SetCreator(string creator, bool isUTF8 = false)
        {
            // Creator of document
            this.metadata["Creator"] = isUTF8 ? creator : UTF8_encode(creator);
        }

        public void SetAliasNbPages(string alias = "{nb}")
        {
            // Define an alias for total number of pages
            this.AliasNbPages = alias;
        }

        private string UTF8_encode(string str)
        {
            byte[] bytes = Encoding.Default.GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

        public void SetDrawColor(double r, double? g = null, double? b = null)
        {
            // Set color for all stroking operations
            if((r == 0 && g == 0 && b == 0) || g == null) {
                double val = r/255;
                this.DrawColor =  $"{val:F3} G";
            } else {
                this.DrawColor = $"{r/255:F3} {g/255:F3} {b/255:F3} RG";
            }

            if(this.CurrentPageNo > 0)
                this.Out(this.DrawColor);
        }

        public void SetFillColor(double r, double? g = null, double? b = null)
        {
            // Set color for all filling operations
            if((r == 0 && g == 0 && b == 0) || g == null)
                this.FillColor = $"{r/255:F3} g";
            else
                this.FillColor = $"{r/255:F3} {g/255:F3} {b/255:F3} rg";
            
            this.ColorFlag = (this.FillColor != this.TextColor);
            if(this.CurrentPageNo > 0)
                this.Out(this.FillColor);
        }

        public void SetTextColor(double r, double? g = null, double? b = null)
        {
            // Set color for text
            if((r == 0 && g == 0 && b == 0) || g == null) {
                this.TextColor = $"{r/255:F3} g";
            } else
                this.TextColor = $"{r/255:F3} {g/255:F3} {b/255:F3} rg";
            this.ColorFlag = (this.FillColor != this.TextColor);
        }

        private void Out(string s)
        {
            // Add a line to the document
            switch (this.state)
            {
                case 0:
                    this.Error("No page has been added yet");
                    break;
                case 1:
                    this.Put(s);
                    break;
                case 2:
                    this.pages[this.CurrentPageNo] += $"{s}\n";
                    break;
                case 3:
                    this.Error("The document is closed");
                    break;

                default:
                break;
            }
        }

        private void Put(string s) => this.buffer.Append($"{s}\n");

        private int GetOffSet()
        {
            return this.buffer.Length;
        }

        private void NewObj(int? n = null)
        {
            // Begin a new object
            if(n == null) {
                n = ++this.CurrentObjectNo;
            } 
            this.offsets[(int) n] = this.GetOffSet();
            this.Put($"{n} 0 obj");
        }

        protected void EndObj()
        {
            this.Put("endobj");
        }

        public bool AcceptPageBreak()
        {
            // Accept automatic page break or not
            return this.AutoPageBreak;
        }

        public void Cell(
            double w,
            double h = 0, 
            string txt = "", 
            string border = "0", 
            double ln = 0, 
            string align = "", 
            bool fill = false, 
            string link = ""
        ) {
            // Output a cell
            double k = this.ScaleFactor;
            double x, y;
            if(this.CurrentY + h > this.PageBreakTrigger
               && !this.InHeader
               && !this.InFooter
               && this.AcceptPageBreak())
            {
                // Automatic page break
                x = this.CurrentX;
                double ws = this.WordSpacing;
                if(ws > 0)
                {
                    this.WordSpacing = 0;
                    this.Out("0 Tw");
                }
                this.AddPage(this.CurOrientation, this.CurPageSize, this.CurRotation);
                this.CurrentX = x;
                if(ws > 0) {
                    this.WordSpacing = ws;
                    this.Out($"{ws * k:F3} Tw");
                }
            }
            if(w == 0) {
                w = this.CurrentPageWidth - this.RightMargin - this.CurrentX;
            }
            var s = "";
            var op = "";
            if(fill || border == "1") {
                if(fill) {
                    op = (border == "1") ? "B" : "f";
                } else {
                    op = "S";
                }
                s = $"{this.CurrentX * k:F2} {(this.CurrentPageHeight - this.CurrentY) * k:F2} {w * k:F2} {-h * k:F2} re {op} ";
            }
            
            x = this.CurrentX;
            y = this.CurrentY;
            if(border.IndexOf("L") > -1)
                s += $"{x * k:F2} {(this.CurrentPageHeight - y) * k:F2} m {x * k:F2} {(this.CurrentPageHeight - (y + h)) * k:F2} l S";
            if(border.IndexOf("T") > -1)
                s += $"{x * k:F2} {(this.CurrentPageHeight - y) * k:F2} m {(x + w) * k:F2} {(this.CurrentPageHeight - y) * k:F2} l S";
            if(border.IndexOf("R") > -1)
                s += $"{(x + w) * k:F2} {(this.CurrentPageHeight - y) * k:F2} m {(x + w) * k:F2} {(this.CurrentPageHeight - (y + h)) * k:F2} l S";
            if(border.IndexOf("B") > -1)
                s += $"{x * k:F2} {(this.CurrentPageHeight - (y + h)) * k:F2} m {(x + w) * k:F2} {(this.CurrentPageHeight - (y + h)) * k:F2} l S";

            if(! string.IsNullOrEmpty(txt))
            {
                double dx = this.CellMargin;
                if(this.CurrentFont.GetType() != typeof(Font))
                    this.Error("No font has been set");
                if(align == "R")
                    dx = w - this.CellMargin - this.GetStringWidth(txt);
                else if(align == "C")
                    dx = (w - this.GetStringWidth(txt)) / 2;
                
                if(this.ColorFlag)
                    s += $"q {this.TextColor} ";
                s += $"BT {(this.CurrentX+dx)*k:F2} {(this.CurrentPageHeight - (this.CurrentY + 0.5 * h + 0.3 * this.FontSize)) * k:F2} Td ({this.Escape(txt)}) Tj ET";
                if(this.underline)
                    s += " " + this.DounderLine(this.CurrentX + dx, this.CurrentY + 0.5 * h + 0.3 * this.FontSize, txt);
                if(this.ColorFlag)
                    s += " Q";
                // if(link)
                //     this.Link(this.CurrentX+dx,this.CurrentY+.5*h-.5*this.FontSize,this.GetStringWidth(txt),this.FontSize,link);
            }
            if(! string.IsNullOrEmpty(s)) {
                this.Out(s);
            }
            this.lasth = h;
            if(ln > 0)
            {
                // Go to next line
                this.CurrentY += h;
                if(ln == 1)
                    this.CurrentX = this.LeftMargin;
            }
            else
                this.CurrentX += w;
        }

        protected string DounderLine(double x, double y, string txt)
        {
            // Underline text
            var up = this.CurrentFont.up;
            var ut = this.CurrentFont.ut;
            var w = this.GetStringWidth(txt) + this.WordSpacing * txt.Count(x => x == ' ');
            return $"{x * this.ScaleFactor:F2} {(this.CurrentPageHeight - (y - up/1000 * this.FontSize)) * this.ScaleFactor:F2} {w * this.ScaleFactor:F2} {-ut/1000 * this.FontSizePt:F2} re f";
        }

        private string Escape(string s)
        {
            // Escape special characters
            s = s.Replace("\\","\\\\");
            s = s.Replace("(","\\(");
            s = s.Replace(")","\\)");
            s = s.Replace("\r","\\r");
            s = s.Replace("\n","\\n");
            s = s.Replace("\t","\\t");
            return s;
        }

        private double GetStringWidth(string s)
        {
            // Get width of a string in the current font
            var cw = this.CurrentFont.cw;
            var w = 0;
            var l = s.Length;
            for(var i = 0; i < l; i++) {
                w += cw.Where(x => x.Item1 == s[i]).FirstOrDefault().Item2;
            }

            return w * this.FontSize / 1000;
        }

        public void SetLineWidth(double width)
        {
            // Set line width
            this.LineWidth = width;
            if(this.CurrentPageNo > 0)
                this.Out($"{width * this.ScaleFactor:F2} w");
        }

        public void Line(double x1, double y1, double x2, double y2)
        {
            // Draw a line
            this.Out( $"{x1 * this.ScaleFactor:F2} {(this.CurrentPageHeight - y1) * this.ScaleFactor:F2} m {x2 * this.ScaleFactor:F2} {(this.CurrentPageHeight - y2) * this.ScaleFactor:F2} l S");
        }

        public void Rect(double x,
                         double y,
                         double w,
                         double h,
                         string style = "")
        {
            // Draw a rectangle
            var op = "S";
            if(style == "F")
                op = "f";
            else if(style == "FD" || style == "DF")
                op = "B";

            this.Out($"{x * this.ScaleFactor:F2} {(this.CurrentPageHeight - y) * this.ScaleFactor:F2} {w * this.ScaleFactor:F2} {-h * this.ScaleFactor:F2} re {op}");
        }

        public void SetFontSize(double size)
        {
            // Set font size in points
            if(this.FontSizePt == size)
                return;
            this.FontSizePt = size;
            this.FontSize = size / this.ScaleFactor;
            if(this.CurrentPageNo > 0)
                this.Out($"BT /F{this.CurrentFont.i} {this.FontSizePt:F2} Tf ET");
        }

        public double GetPageWidth()
        {
            return this.CurrentPageWidth;
        }

        public double  GetPageHeight()
        {
            return this.CurrentPageHeight;
        }

        public double GetX()
        {
            return this.CurrentX;
        }

        public void SetX(double x)
        {
            // Set x position
            if(x >= 0)
                this.CurrentX = x;
            else
                this.CurrentX = this.CurrentPageWidth + x;
        }

        public double GetY()
        {
            return this.CurrentY;
        }

        public void SetY(double y, bool resetX = true)
        {
            // Set y position and optionally reset x
            if(y >= 0)
                this.CurrentY = y;
            else
                this.CurrentY = this.CurrentPageHeight + y;
            if(resetX)
                this.CurrentX = this.LeftMargin;
        }

        public void SetXY(double x, double y)
        {
            this.SetX(x);
            this.SetY(y, false);
        }

        public void AddPage(
            string orientation = "", 
            dynamic? size = null, 
            double rotation = 0
        )
        {
            // Start a new page
            if(this.state == 3)
                this.Error("The document is closed");
            var family = this.FontFamily;
            var style = this.FontStyle + (this.underline ? "U" : "");
            var fontsize = this.FontSizePt;
            var lw = this.LineWidth;
            var dc = this.DrawColor;
            var fc = this.FillColor;
            var tc = this.TextColor;
            var cf = this.ColorFlag;
            if(this.CurrentPageNo > 0)
            {
                // Page footer
                this.InFooter = true;
                this.Footer();
                this.InFooter = false;
                // Close page
                this.EndPage();
            }
            // Start new page
            this.BeginPage(orientation, size, rotation);
            // Set line cap style to square
            this.Out("2 J");
            // Set line width
            this.LineWidth = lw;
            this.Out($"{lw * this.ScaleFactor:F2} w");
            // Set font
            if(! string.IsNullOrEmpty(family))
                this.SetFont(family, style, fontsize);
            // Set colors
            this.DrawColor = dc;
            if(dc != "0 G")
                this.Out(dc);

            this.FillColor = fc;
            if(fc != "0 g")
                this.Out(fc);

            this.TextColor = tc;
            this.ColorFlag = cf;
            // Page header
            this.InHeader = true;
            this.Header();
            this.InHeader = false;
            // Restore line width
            if(this.LineWidth != lw)
            {
                this.LineWidth = lw;
                this.Out($"{lw * this.ScaleFactor:F2} w");
            }
            // Restore font
            if(! string.IsNullOrEmpty(family))
                this.SetFont(family, style, fontsize);
            // Restore colors
            if(this.DrawColor != dc)
            {
                this.DrawColor = dc;
                this.Out(dc);
            }
            if(this.FillColor != fc)
            {
                this.FillColor = fc;
                this.Out(fc);
            }
            this.TextColor = tc;
            this.ColorFlag = cf;
        }

        public void Header()
        {
            // To be implemented in your own inherited class
        }

        public void Footer()
        {
            // To be implemented in your own inherited class
        }

        public int PageNo()
        {
            return this.CurrentPageNo;
        }

        protected void EndPage()
        {
            this.state = 1;
        }

        protected void BeginPage(
            string orientation, 
            dynamic size, 
            double rotation
        )
        {
            this.CurrentPageNo++;
            this.pages[this.CurrentPageNo] = "";
            this.state = 2;
            this.CurrentX = this.LeftMargin;
            this.CurrentY = this.TopMargin;
            this.FontFamily = "";
            var _size = this.DefPageSize;
            // Check page size and orientation
            if(orientation == "")
                orientation = this.DefOrientation;
            else
                orientation = orientation.Substring(0, 1).ToUpper();
            if(size == null)
                _size = this.DefPageSize;
            else
                _size = this.GetPageSize(size);
            if(
                   orientation != this.CurOrientation 
                || _size.Width != this.CurPageSize.Width 
                || _size.Height != this.CurPageSize.Height
            )
            {
                // New size or orientation
                if(orientation == "P")
                {
                    this.CurrentPageWidth = _size.Width;
                    this.CurrentPageHeight = _size.Height;
                }
                else
                {
                    this.CurrentPageWidth = _size.Height;
                    this.CurrentPageHeight = _size.Width;
                }
                this.wPt = this.CurrentPageWidth * this.ScaleFactor;
                this.hPt = this.CurrentPageHeight * this.ScaleFactor;
                this.PageBreakTrigger = this.CurrentPageHeight - this.BottomMargin;
                this.CurOrientation = orientation;
                this.CurPageSize = _size;
            }
            if(
                   orientation != this.DefOrientation 
                || _size.Width != this.DefPageSize.Width 
                || _size.Height != this.DefPageSize.Height
            ) {
                this.PageInfo[this.CurrentPageNo]["size"] = (this.wPt, this.hPt);
            }
            if(rotation != 0)
            {
                if(rotation % 90 != 0) {
                    this.Error($"Incorrect rotation value: {rotation}");
                }
                this.CurRotation = rotation;
                this.PageInfo[this.CurrentPageNo]["rotation"] = rotation;
            }
        }

        public void SetFont(
            string family, 
            string style = "", 
            double size = 0
        )
        {
            // Select a font; size given in points
            if(family == "") {
                family = this.FontFamily;
            } else {
                family = family.ToLower();
            }
            style = style.ToUpper();
            if(style.IndexOf("U") > -1) {
                this.underline = true;
                style = style.Replace("U", "");
            } else
                this.underline = false;
            if(style == "IB")
                style = "BI";
            if(size == 0)
                size = this.FontSizePt;
            // Test if font is already selected
            if(
                   this.FontFamily == family 
                && (string) this.FontStyle == style 
                && this.FontSizePt == size
            )
                return;

            // Test if font is already loaded
            var fontkey = $"{family}{style}".ToLower();
            if(! this.fonts.ContainsKey(fontkey))
            {
                // Test if one of the core fonts
                if(family == "arial")
                    family = "helvetica";
                if(this.CoreFonts.Contains(family))
                {
                    if(family == "symbol" || family == "zapfdingbats")
                        style = "";
                    fontkey = $"{family}{style}".ToLower();
                    if(! this.fonts.ContainsKey(fontkey))
                        this.AddFont(family, style);
                }
                else
                    this.Error($"Undefined font: {family} {style}");
            }
            // Select it
            this.FontFamily = family;
            this.FontStyle = style;
            this.FontSizePt = size;
            this.FontSize = size / this.ScaleFactor;
            this.CurrentFont = this.fonts[fontkey];
            if(this.CurrentPageNo > 0)
                this.Out($"BT /F{this.CurrentFont.i} {this.FontSizePt:F2} Tf ET");
        }

        public void AddFont(string family, string style = "")
        {
            // Add a TrueType, OpenType or Type1 font
            family = family.ToLower();
            style = style.ToUpper();
            if(style == "IB")
                style = "BI";
            
            var fontkey = $"{family}{style}".ToLower();
            if(this.fonts.ContainsKey(fontkey))
                return;

            var font = Fonts.GetFont(fontkey);
            font.i = this.fonts.Count + 1;
            
            this.fonts[fontkey] = font;
        }

        public void Output(
            string dest = "", 
            string name = "", 
            bool isUTF8 = false
        )
        {
            // Output PDF to some destination
            this.Close();
            if(name.Length == 1 && dest.Length != 1)
            {
                // Fix parameter order
                var tmp = dest;
                dest = name;
                name = tmp;
            }
            if(dest == "")
                dest = "I";
            if(name == "")
                name = "doc.pdf";
            switch(dest.ToUpper())
            {
                case "F":
                    // Save to local file
                    try {
                        var myByteArray = System.Text.Encoding.UTF8.GetBytes(this.buffer.ToString());
                        using(var f = new FileStream(name, FileMode.Create))
                        {
                            f.Write(myByteArray, 0, myByteArray.Length);
                        }
                        // var ms = new MemoryStream(myByteArray);
                        // File.WriteAllBytes(name, myByteArray);
                    } catch (System.Exception) {
                        this.Error("Unable to create output file: " + name);
                    }
                    break;
                case "S":
                    // Return as a string
                    // return this.buffer;
                    break;
                default:
                    this.Error("Incorrect output destination: " + dest);
                    break;
            }
        }

        public void Close()
        {
            // Terminate document
            if(this.state == 3)
                return;
            if(this.CurrentPageNo == 0)
                this.AddPage();
            // Page footer
            this.InFooter = true;
            this.Footer();
            this.InFooter = false;
            // Close page
            this.EndPage();
            // Close document
            this.EndDoc();
        }

        protected void PutPage(int n)
        {
            this.NewObj();
            this.Put("<</Type /Page");
            this.Put("/Parent 1 0 R");

            if(this.PageInfo[n].ContainsKey("size"))
                this.Put($"/MediaBox [0 0 {this.PageInfo[n]["size"].Item1:F2} {this.PageInfo[n]["size"].Item2:F2}]");
            if(this.PageInfo[n].ContainsKey("rotation"))
                this.Put($"/Rotate {this.PageInfo[n]["rotation"]}");
            this.Put("/Resources 2 0 R");
            // if(isset(this.PageLinks[n]))
            // {
            //     // Links
            //     $annots = "/Annots [";
            //     foreach(this.PageLinks[$n] as $pl)
            //     {
            //         $rect = sprintf("%.2F %.2F %.2F %.2F",$pl[0],$pl[1],$pl[0]+$pl[2],$pl[1]-$pl[3]);
            //         $annots += "<</Type /Annot /Subtype /Link /Rect [".$rect."] /Border [0 0 0] ";
            //         if(is_string($pl[4]))
            //             $annots += "/A <</S /URI /URI ".this.TextString($pl[4]).">>>>";
            //         else
            //         {
            //             $l = this.links[$pl[4]];
            //             if(isset(this.PageInfo[$l[0]]["size"]))
            //                 $h = this.PageInfo[$l[0]]["size"][1];
            //             else
            //                 $h = (this.DefOrientation=="P") ? this.DefPageSize[1]*this.ScaleFactor : this.DefPageSize[0]*this.ScaleFactor;
            //             $annots += sprintf("/Dest [%d 0 R /XYZ 0 %.2F null]>>",this.PageInfo[$l[0]]["n"],$h-$l[1]*this.ScaleFactor);
            //         }
            //     }
            //     this.Put($annots."]");
            // }
            if(this.WithAlpha)
                this.Put("/Group <</Type /Group /S /Transparency /CS /DeviceRGB>>");
            this.Put($"/Contents {this.CurrentObjectNo + 1} 0 R>>");
            this.EndObj();
            // Page content
            if(! string.IsNullOrEmpty(this.AliasNbPages))
                this.pages[n] = this.pages[n].Replace(this.AliasNbPages, $"{this.CurrentPageNo}");
            this.PutStreamObject(this.pages[n]);
        }

        protected void PutPages()
        {
            var nb = this.CurrentPageNo;
            for(var n = 1; n <= nb; n++) {
                this.PageInfo[n]["n"] = this.CurrentObjectNo + 1 + (2 * (n - 1));
            }
            for(var n = 1; n <= nb; n++) {
                this.PutPage(n);
            }
            // Pages root
            this.NewObj(1);
            this.Put("<</Type /Pages");
            var kids = "/Kids [";
            for(var n = 1; n <= nb; n++) {
                kids += this.PageInfo[n]["n"] + " 0 R ";
            }
            this.Put($"{kids}]");
            this.Put($"/Count {nb}");

            double w, h; 
            if(this.DefOrientation == "P") {
                w = this.DefPageSize.Width;
                h = this.DefPageSize.Height;
            } else {
                w = this.DefPageSize.Height;
                h = this.DefPageSize.Width;
            }
            this.Put($"/MediaBox [0 0 {w * this.ScaleFactor:F2} {h * this.ScaleFactor:F2}]");
            this.Put(">>");
            this.EndObj();
        }

        protected void PutHeader()
        {
            this.Put($"%PDF-{this.PDFVersion}");
        }

        protected void PutTrailer()
        {
            this.Put($"/Size {this.CurrentObjectNo + 1}");
            this.Put($"/Root {this.CurrentObjectNo} 0 R");
            this.Put($"/Info {this.CurrentObjectNo - 1} 0 R");
        }

        protected void EndDoc()
        {
            this.PutHeader();
            this.PutPages();
            this.PutResources();
            // Info
            this.NewObj();
            this.Put("<<");
            this.PutInfo();
            this.Put(">>");
            this.EndObj();
            // Catalog
            this.NewObj();
            this.Put("<<");
            this.PutCatalog();
            this.Put(">>");
            this.EndObj();
            // Cross-ref
            var offset = this.GetOffSet();
            this.Put("xref");
            this.Put("0 " + (this.CurrentObjectNo + 1));
            this.Put("0000000000 65535 f ");
            for(var i = 1; i <= this.CurrentObjectNo; i++)
                this.Put($"{this.offsets[i]:D10} 00000 n ");
            // Trailer
            this.Put("trailer");
            this.Put("<<");
            this.PutTrailer();
            this.Put(">>");
            this.Put("startxref");
            this.Put(offset.ToString());
            this.Put("%%EOF");
            this.state = 3;
        }

        protected void PutXObjectDict()
        {
            // TODO: Implement this
            // foreach(var image in this.images)
            //     this.Put("/I" + image["i"] + " " + image["n"] + " 0 R");
        }

        protected void PutResourceDict()
        {
            this.Put("/ProcSet [/PDF /Text /ImageB /ImageC /ImageI]");
            this.Put("/Font <<");
            foreach(var font in this.fonts) {
                var f = font.Value;
                this.Put("/F" + f.i + " " + f.n + " 0 R");
            }
            this.Put(">>");
            this.Put("/XObject <<");
            this.PutXObjectDict();
            this.Put(">>");
        }

        protected void PutResources()
        {
            this.PutFonts();
            // TODO: Implement this
            // this._putimages();
            // Resource dictionary
            this.NewObj(2);
            this.Put("<<");
            this.PutResourceDict();
            this.Put(">>");
            this.EndObj();
        }

        protected void PutInfo()
        {
            this.metadata["Producer"] = "NetPdf " + this.NetPdfVersion;
            this.metadata["CreationDate"] = $"D:{DateTime.Now:yyyyMMddHmmss}";
            foreach(var (key, value) in this.metadata)
                this.Put($"/{key} "+ this.TextString(value));
        }

        protected string TextString(string s)
        {
            // Format a text string
            // if(!this._isascii($s))
            //     $s = this._UTF8toUTF16($s);
            // return '('.this.Escape($s).')';
            return $"({s})";
        }

        protected void PutFonts()
        {
            // foreach(this.FontFiles as $file=>$info)
            // {
            //     // Font file embedding
            //     this.NewObj();
            //     this.FontFiles[$file]["n"] = this.CurrentObjectNo;
            //     $font = file_get_contents(this.fontpath.$file,true);
            //     if(!$font)
            //         this.Error("Font file not found: ".$file);
            //     $compressed = (substr($file,-2)==".z");
            //     if(!$compressed && isset($info["length2"]))
            //         $font = substr($font,6,$info["length1"]).substr($font,6+$info["length1"]+6,$info["length2"]);
            //     this.Put("<</Length ".strlen($font));
            //     if($compressed)
            //         this.Put("/Filter /FlateDecode");
            //     this.Put("/Length1 ".$info["length1"]);
            //     if(isset($info["length2"]))
            //         this.Put("/Length2 ".$info["length2"]." /Length3 0");
            //     this.Put(">>");
            //     this.PutStream($font);
            //     this.EndObj();
            // }
            foreach(var f in this.fonts)
            {
                var font = f.Value;
                var k = f.Key;
                // Encoding
                // if(isset($font["diff"]))
                // {
                //     if(!isset(this.encodings[$font["enc"]]))
                //     {
                //         this.NewObj();
                //         this.Put("<</Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [".$font["diff"]."]>>");
                //         this.EndObj();
                //         this.encodings[$font["enc"]] = this.CurrentObjectNo;
                //     }
                // }
                // ToUnicode CMap
                
                var cmapkey = font.enc;
                    
                if(! this.cmaps.ContainsKey(cmapkey))
                {
                    var cmap = this.ToUnicodeCmap(font.uv);
                    this.PutStreamObject(cmap);
                    this.cmaps[cmapkey] = this.CurrentObjectNo;
                }
                
                // Font object
                this.fonts[k].n = this.CurrentObjectNo+1;
                var type = font.type;
                var name = font.name;
                // TODO: Implement this
                // if($font["subsetted"])
                //     $name = "AAAAAA+".$name;
                if(type == "Core")
                {
                    // Core font
                    this.NewObj();
                    this.Put("<</Type /Font");
                    this.Put("/BaseFont /" + name);
                    this.Put("/Subtype /Type1");
                    if(name != "Symbol" && name != "ZapfDingbats")
                        this.Put("/Encoding /WinAnsiEncoding");
                    // if(isset($font["uv"]))
                        this.Put("/ToUnicode " + this.cmaps[cmapkey] + " 0 R");
                    this.Put(">>");
                    this.EndObj();
                }
                // else if($type=="Type1" || $type=="TrueType")
                // {
                //     // Additional Type1 or TrueType/OpenType font
                //     this.NewObj();
                //     this.Put("<</Type /Font");
                //     this.Put("/BaseFont /".$name);
                //     this.Put("/Subtype /".$type);
                //     this.Put("/FirstChar 32 /LastChar 255");
                //     this.Put("/Widths ".(this.CurrentObjectNo+1)." 0 R");
                //     this.Put("/FontDescriptor ".(this.CurrentObjectNo+2)." 0 R");
                //     if(isset($font["diff"]))
                //         this.Put("/Encoding ".this.encodings[$font["enc"]]." 0 R");
                //     else
                //         this.Put("/Encoding /WinAnsiEncoding");
                //     if(isset($font["uv"]))
                //         this.Put("/ToUnicode ".this.cmaps[$cmapkey]." 0 R");
                //     this.Put(">>");
                //     this.EndObj();
                //     // Widths
                //     this.NewObj();
                //     $cw = &$font["cw"];
                //     $s = "[";
                //     for($i=32;$i<=255;$i++)
                //         $s += $cw[chr($i)]." ";
                //     this.Put($s."]");
                //     this.EndObj();
                //     // Descriptor
                //     this.NewObj();
                //     $s = "<</Type /FontDescriptor /FontName /".$name;
                //     foreach($font["desc"] as $k=>$v)
                //         $s += " /".$k." ".$v;
                //     if(!empty($font["file"]))
                //         $s += " /FontFile".($type=="Type1" ? "" : "2")." ".this.FontFiles[$font["file"]]["n"]." 0 R";
                //     this.Put($s.">>");
                //     this.EndObj();
                // }
                // else
                // {
                //     // Allow for additional types
                //     $mtd = "_put".strtolower($type);
                //     if(!method_exists($this,$mtd))
                //         this.Error("Unsupported font type: ".$type);
                //     this.$mtd($font);
                // }
            }
        }
        protected void PutCatalog()
        {
            var n = this.PageInfo[1]["n"];
            this.Put("/Type /Catalog");
            this.Put("/Pages 1 0 R");
            if(this.ZoomMode == "fullpage")
                this.Put($"/OpenAction [{n} 0 R /Fit]");
            else if(this.ZoomMode == "fullwidth")
                this.Put($"/OpenAction [{n} 0 R /FitH null]");
            else if(this.ZoomMode == "real")
                this.Put($"/OpenAction [{n} 0 R /XYZ null null 1]");
            // else if(!is_string(this.ZoomMode))
            //     this.Put($"/OpenAction [{n} 0 R /XYZ null null {this.ZoomMode/100:F2}]");
            if(this.LayoutMode == "single")
                this.Put("/PageLayout /SinglePage");
            else if(this.LayoutMode == "continuous")
                this.Put("/PageLayout /OneColumn");
            else if(this.LayoutMode == "two")
                this.Put("/PageLayout /TwoColumnLeft");
        }

        protected void PutStream(string data)
        {
            this.Put("stream");
            this.Put(data);
            this.Put("endstream");
        }

        protected void PutStreamObject(string data)
        {
            var entries = "";
            entries += "/Length " + data.Length;
            this.NewObj();
            this.Put("<<" + entries + ">>");
            this.PutStream(data);
            this.EndObj();
        }

        protected string ToUnicodeCmap(List<(int, dynamic)> uv)
        {
            var ranges = "";
            var nbr = 0;
            var chars = "";
            var nbc = 0;
            foreach(var (c, v) in uv)
            {
                var v_array = v as IEnumerable; 

                if (v is System.Runtime.CompilerServices.ITuple) {
                    ranges += $"<{c:X2}> <{c + v.Item2 - 1:X2}> <{v.Item1:X4}>\n";
                    nbr++;
                } else {
                    chars += $"<{c:X2}> <{v:X4}>\n";
                    nbc++;
                }
            }

            var s = "/CIDInit /ProcSet findresource begin\n";
            s += "12 dict begin\n";
            s += "begincmap\n";
            s += "/CIDSystemInfo\n";
            s += "<</Registry (Adobe)\n";
            s += "/Ordering (UCS)\n";
            s += "/Supplement 0\n";
            s += ">> def\n";
            s += "/CMapName /Adobe-Identity-UCS def\n";
            s += "/CMapType 2 def\n";
            s += "1 begincodespacerange\n";
            s += "<00> <FF>\n";
            s += "endcodespacerange\n";
            if(nbr > 0) {
                s += $"{nbr} beginbfrange\n";
                s += ranges;
                s += "endbfrange\n";
            }
            if(nbc > 0) {
                s += $"{nbc} beginbfchar\n";
                s += chars;
                s += "endbfchar\n";
            }
            s += "endcmap\n";
            s += "CMapName currentdict /CMap defineresource pop\n";
            s += "end\n";
            s += "end";
            return s;
        }
    }
}