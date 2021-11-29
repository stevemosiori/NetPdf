using System.Reflection;
using System.Text;

namespace Tephanik
{
    public class NetPdf
    {
        private int page;               // current page number
        private int n;                  // current object number
        private List<int>? offsets;            // array of object offsets
        private StringBuilder buffer;             // buffer holding in-memory PDF
        private List<string> pages;              // array containing pages
        private int state;              // current document state
        private bool compress;           // compression flag
        private double k;                  // scale factor (number of points in user unit)
        private string DefOrientation;     // default orientation
        private string CurOrientation;     // current orientation
        private dynamic StdPageSizes;       // standard page sizes
        private (double, double) DefPageSize;        // default page size
        private (double, double) CurPageSize;        // current page size
        private double CurRotation;        // current page rotation
        private List<dynamic> PageInfo;           // page-related data
        private double wPt, hPt;          // dimensions of current page in points
        private double w, h;              // dimensions of current page in user unit
        private double lMargin;            // left margin
        private double tMargin;            // top margin
        private double rMargin;            // right margin
        private double bMargin;            // page break margin
        private double cMargin;            // cell margin
        private double x, y;              // current position in user unit
        private double lasth;              // height of last printed cell
        private double LineWidth;          // line width in user unit
        private string? fontpath;           // path containing fonts
        private List<string> CoreFonts;          // array of core font names
        private Dictionary<string, Font> fonts;              // array of used fonts
        private object? FontFiles;          // array of font files
        private object? encodings;          // array of encodings
        private object? cmaps;              // array of ToUnicode CMaps
        private string FontFamily;         // current font family
        private string FontStyle;          // current font style
        private bool underline;          // underlining flag
        private Font CurrentFont;        // current font info
        private double FontSizePt;         // current font size in points
        private double FontSize;           // current font size in user unit
        private string? DrawColor;          // commands for drawing color
        private string? FillColor;          // commands for filling color
        private string? TextColor;          // commands for text color
        private bool ColorFlag;          // indicates whether fill and text colors are different
        private bool WithAlpha;          // indicates whether alpha channel is used
        private double ws;                 // word spacing
        private List<object>? images;             // array of used images
        private List<object>? PageLinks;          // array of links in pages
        private List<object>? links;              // array of internal links
        private bool AutoPageBreak;      // automatic page breaking
        private double PageBreakTrigger;   // threshold used to trigger page breaks
        private bool InHeader;           // flag set when processing header
        private bool InFooter;           // flag set when processing footer
        private string? AliasNbPages;       // alias for total number of pages
        private string? ZoomMode;           // zoom display mode
        private string? LayoutMode;         // layout display mode
        private Dictionary<string, dynamic> metadata;           // document properties
        private string PDFVersion;         // PDF version number

        public NetPdf(string orientation="P", string unit="mm", string inputSize="A4")
        {
            // Some checks
            // this._dochecks();
            // Initialization of properties
            this.state = 0;
            this.page = 0;
            this.n = 2;
            this.buffer = new StringBuilder();
            this.pages = new();
            this.PageInfo = new();
            this.fonts =   new();
            this.FontFiles = new();
            this.encodings = new();
            this.cmaps = new();
            this.images = new();
            this.links = new();
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
            this.ws = 0;
            // Font path
            // if(defined('FPDF_FONTPATH'))
            // {
            //     this.fontpath = FPDF_FONTPATH;
            //     if(substr(this.fontpath,-1)!='/' && substr(this.fontpath,-1)!='\\')
            //         this.fontpath .= '/';
            // }
            // else if(is_dir(dirname(__FILE__).'/font'))
            //     this.fontpath = dirname(__FILE__).'/font/';
            // else
            this.fontpath = "";
            // Core fonts
            this.CoreFonts = new List<string>{
                "courier", "helvetica", "times", "symbol", "zapfdingbats"
            };

            // Scale factor
            if(unit.Equals("pt")) {
                this.k = 1;
            } else if(unit.Equals("mm")) {
                this.k = 72/25.4;
            } else if(unit.Equals("cm")) {
                this.k = 72/2.54;
            } else if(unit.Equals("in")) {
                this.k = 72;
            } else {
                this.Error($"Incorrect unit: {unit.ToString()}");
            }

            // Page sizes
            this.StdPageSizes = new {
                a3      = (841.89,  1190.55),
                a4      = (595.28,  841.89),
                a5      = (420.94,  595.28),
                letter  = (612.0,   792.0),
                legal   = (612.0,   1008.0)
            };

            (double, double) size = this.GetPageSize(inputSize);
            this.DefPageSize = size;
            this.CurPageSize = size;
            // Page orientation
            orientation = orientation.ToLower();
            this.DefOrientation = "";
            if(orientation.Equals("p") || orientation.Equals("portrait")) {
                this.DefOrientation = "P";
                this.w = size.Item1;
                this.h = size.Item2;
            } else if(orientation.Equals("l") || orientation.Equals("landscape")) {
                this.DefOrientation = "L";
                this.w = size.Item2;
                this.h = size.Item1;
            } else {
                this.Error($"Incorrect orientation: {orientation.ToString()}");
            }

            this.CurOrientation = this.DefOrientation;
            this.wPt = this.w*this.k;
            this.hPt = this.h*this.k;
            // Page rotation
            this.CurRotation = 0.0;
            // Page margins (1 cm)
            double margin = 28.35/this.k;
            this.SetMargins(margin, margin);
            // Interior cell margin (1 mm)
            this.cMargin = margin/10;
            // Line width (0.2 mm)
            this.LineWidth = .567/this.k;
            // Automatic page break
            this.SetAutoPageBreak(true,2*margin);
            // Default display mode
            this.SetDisplayMode("default");
            // Enable compression
            this.SetCompression(true);
            // Set default PDF version number
            this.PDFVersion = "1.3";

            this.metadata = new();
        }

        private (double, double) GetPageSize(dynamic size)
        {
            if(size is string)
            {
                size = size.ToLower();
                if(this.StdPageSizes.GetType().GetProperty(size) == null) {
                    this.Error("Unknown page size: " + size.ToString());
                }
                PropertyInfo info = this.StdPageSizes.GetType().GetProperty(size);
                (double, double) a = info.GetValue(this.StdPageSizes, null);
                return (a.Item1/this.k, a.Item2/this.k);
            } else {
                if(size.Item1 > size.Item2)
                    return (size.Item2, size.Item1);
                else
                    return (size.Item1, size.Item2);
            }
        }

        private void Error(string msg)
        {
            // Fatal error
            throw new Exception("NetPdf error: " + msg);
        }

        public void SetMargins(double left, double top, double? right = null)
        {
            // Set left, top and right margins
            this.lMargin = left;
            this.tMargin = top;
            
            if(right == null) {
                right = left;
            }

            this.rMargin = (double) right;
        }

        public void SetLeftMargin(double margin)
        {
            // Set left margin
            this.lMargin = margin;
            if(this.page > 0 && this.x < margin) {
                this.x = margin;
            }
        }

        public void SetTopMargin(double margin)
        {
            // Set top margin
            this.tMargin = margin;
        }

        public void SetRightMargin(double margin)
        {
            // Set right margin
            this.rMargin = margin;
        }

        public void SetAutoPageBreak(bool auto, double margin = 0.0)
        {
            // Set auto page break mode and triggering margin
            this.AutoPageBreak = auto;
            this.bMargin = margin;
            this.PageBreakTrigger = this.h - margin;
        }

        public void SetDisplayMode(string zoom, string layout = "default")
        {
            // Set display mode in viewer
            if(zoom.Equals("fullpage") || zoom.Equals("fullwidth") || zoom.Equals("real") || zoom.Equals("default") || zoom is string) {
                this.ZoomMode = zoom;
            }  else {
                this.Error($"Incorrect zoom display mode: {zoom.ToString()}");
            }

            if(layout.Equals("single") || layout.Equals("continuous") || layout.Equals("two") || layout.Equals("default"))
                this.LayoutMode = layout;
            else {
                this.Error($"Incorrect layout display mode: {layout.ToString()}");
            }
        }

        public void SetCompression(bool compress)
        {
            // Set page compression
            // if(function_exists('gzcompress'))
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

            if(this.page > 0)
                this._out(this.DrawColor);
        }

        private void _out(string s)
        {
            // Add a line to the document
            if(this.state == 2) {
                this.pages[this.page] += $"{s}\n";
            } else if(this.state ==1 ) {
                this._put(s);
            } else if(this.state == 0) {
                this.Error("No page has been added yet");
            } else if(this.state==3) {
                this.Error("The document is closed");
            }
        }

        private void _put(string s) => this.buffer?.Append($"{s}\n");

        private int _getoffset()
        {
            return this.buffer.Length;
        }

        private void _newobj(int? n = null)
        {
            // Begin a new object
            if(n == null) {
                n = ++this.n;
            } 
            this.offsets[(int) n] = this._getoffset();
            this._put($"{n} 0 obj");
        }

        private void _putstream(string data)
        {
            this._put("stream");
            this._put(data);
            this._put("endstream");
        }

        public bool AcceptPageBreak()
        {
            // Accept automatic page break or not
            return this.AutoPageBreak;
        }

        public void Cell(
            double w, 
            double h=0, 
            string txt="", 
            double border=0, 
            double ln=0, 
            string align="", 
            bool fill=false, 
            string link=""
        ) {
            // Output a cell
            double k = this.k;
            if(this.y + h > this.PageBreakTrigger && !this.InHeader && !this.InFooter && this.AcceptPageBreak())
            {
                // Automatic page break
                double x = this.x;
                double ws = this.ws;
                if(ws > 0)
                {
                    this.ws = 0;
                    this._out("0 Tw");
                }
                this.AddPage(this.CurOrientation, this.CurPageSize, this.CurRotation);
                this.x = x;
                if(ws>0)
                {
                    this.ws = ws;
                    this._out($"{ws*k:F3} Tw");
                }
            }
            if(w==0)
                w = this.w-this.rMargin-this.x;
            var s = "";
            var op = "";
            if(fill || border==1)
            {
                if(fill) {
                    op = (border==1) ? "B" : "f";
                } else
                    op = "S";
                s = $"{this.x*k:F2} {(this.h-this.y)*k:F2} %.2F %.2F re %s " ,,w*k,-h*k,op);
            }
            if(is_string(border))
            {
                x = this.x;
                y = this.y;
                if(strpos(border,'L')!==false)
                    s .= sprintf('%.2F %.2F m %.2F %.2F l S ',x*k,(this.h-y)*k,x*k,(this.h-(y+h))*k);
                if(strpos(border,'T')!==false)
                    s .= sprintf('%.2F %.2F m %.2F %.2F l S ',x*k,(this.h-y)*k,(x+w)*k,(this.h-y)*k);
                if(strpos(border,'R')!==false)
                    s .= sprintf('%.2F %.2F m %.2F %.2F l S ',(x+w)*k,(this.h-y)*k,(x+w)*k,(this.h-(y+h))*k);
                if(strpos(border,'B')!==false)
                    s .= sprintf('%.2F %.2F m %.2F %.2F l S ',x*k,(this.h-(y+h))*k,(x+w)*k,(this.h-(y+h))*k);
            }
            if(txt!=='')
            {
                if(!isset(this.CurrentFont))
                    this.Error('No font has been set');
                if(align=='R')
                    dx = w-this.cMargin-this.GetStringWidth(txt);
                else if(align=='C')
                    dx = (w-this.GetStringWidth(txt))/2;
                else
                    dx = this.cMargin;
                if(this.ColorFlag)
                    s .= 'q '.this.TextColor.' ';
                s .= sprintf('BT %.2F %.2F Td (%s) Tj ET',(this.x+dx)*k,(this.h-(this.y+.5*h+.3*this.FontSize))*k,this._escape(txt));
                if(this.underline)
                    s .= ' '.this._dounderline(this.x+dx,this.y+.5*h+.3*this.FontSize,txt);
                if(this.ColorFlag)
                    s .= ' Q';
                if(link)
                    this.Link(this.x+dx,this.y+.5*h-.5*this.FontSize,this.GetStringWidth(txt),this.FontSize,link);
            }
            if(s)
                this._out(s);
            this.lasth = h;
            if(ln>0)
            {
                // Go to next line
                this.y += h;
                if(ln==1)
                    this.x = this.lMargin;
            }
            else
                this.x += w;
        }

        public void AddPage(string orientation="", dynamic? size = null, double rotation=0)
        {
            // Start a new page
            if(this.state==3)
                this.Error("The document is closed");
            var family = this.FontFamily;
            var style = this.FontStyle + (this.underline ? "U" : "");
            var fontsize = this.FontSizePt;
            var lw = this.LineWidth;
            var dc = this.DrawColor;
            var fc = this.FillColor;
            var tc = this.TextColor;
            var cf = this.ColorFlag;
            if(this.page > 0)
            {
                // Page footer
                this.InFooter = true;
                this.Footer();
                this.InFooter = false;
                // Close page
                this._endpage();
            }
            // Start new page
            this._beginpage(orientation, size, rotation);
            // Set line cap style to square
            this._out("2 J");
            // Set line width
            this.LineWidth = lw;
            this._out($"{lw*this.k:F2} w");
            // Set font
            if(! string.IsNullOrEmpty(family))
                this.SetFont(family, style, fontsize);
            // Set colors
            this.DrawColor = dc;
            if(dc != "0 G")
                this._out(dc);
            this.FillColor = fc;
            if(fc != "0 g")
                this._out(fc);
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
                this._out($"{lw * this.k:F2} w");
            }
            // Restore font
            if(! string.IsNullOrEmpty(family))
                this.SetFont(family, style, fontsize);
            // Restore colors
            if(this.DrawColor!=dc)
            {
                this.DrawColor = dc;
                this._out(dc);
            }
            if(this.FillColor!=fc)
            {
                this.FillColor = fc;
                this._out(fc);
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
            // Get current page number
            return this.page;
        }

        protected void _endpage()
        {
            this.state = 1;
        }

        protected void _beginpage(string orientation, dynamic size, double rotation)
        {
            this.page++;
            this.pages[this.page] = "";
            this.state = 2;
            this.x = this.lMargin;
            this.y = this.tMargin;
            this.FontFamily = "";
            var _size = this.DefPageSize;
            // Check page size and orientation
            if(orientation=="")
                orientation = this.DefOrientation;
            else
                orientation = orientation.Substring(0, 1).ToUpper();
            if(size == "")
                _size = this.DefPageSize;
            else
                _size = this.GetPageSize(size);
            if(
                   orientation != thi.CurOrientation 
                || _size.Item1 != this.CurPageSize.Item1 
                || _size.Item2 != this.CurPageSize.Item2
            )
            {
                // New size or orientation
                if(orientation == "P")
                {
                    this.w = _size.Item1;
                    this.h = _size.Item2;
                }
                else
                {
                    this.w = _size.Item2;
                    this.h = _size.Item1;
                }
                this.wPt = this.w*this.k;
                this.hPt = this.h*this.k;
                this.PageBreakTrigger = this.h - this.bMargin;
                thi.CurOrientation = orientation;
                this.CurPageSize = _size;
            }
            if(
                   orientation != this.DefOrientation 
                || _size.Item1 != this.DefPageSize.Item1 
                || _size.Item2 != this.DefPageSize.Item2
            ) {
                this.PageInfo[this.page]["size"] = (this.wPt, this.hPt);
            }
            if(rotation != 0)
            {
                if(rotation % 90 != 0) {
                    this.Error("Incorrect rotation value: " + rotation.ToString());
                }
                this.CurRotation = rotation;
                this.PageInfo[this.page]["rotation"] = rotation;
            }
        }

        public void SetFont(string family, string style = "", double size = 0)
        {
            // Select a font; size given in points
            if(family == "")
                family = this.FontFamily;
            else
                family = family.ToLower();
            style = style.ToUpper();
            if(style.IndexOf("U") != -1)
            {
                this.underline = true;
                style = style.Replace("U", "");
            }
            else
                this.underline = false;
            if(style == "IB")
                style = "BI";
            if(size == 0)
                size = this.FontSizePt;
            // Test if font is already selected
            if(
                   this.FontFamily == family 
                && (string) this.FontStyle == style 
                && this.FontSizePt==size
            )
                return;
            // Test if font is already loaded
            var fontkey = $"{family}{style}";
            if(! this.fonts.ContainsKey(fontkey))
            {
                // Test if one of the core fonts
                if(family == "arial")
                    family = "helvetica";
                if(this.CoreFonts.Contains(family))
                {
                    if(family=="symbol" || family=="zapfdingbats")
                        style = "";
                    fontkey = $"{family}{style}";
                    if(! this.fonts.ContainsKey(fontkey))
                        this.AddFont(family, style);
                }
                else
                    this.Error("Undefined font: " + family + " " + style);
            }
            // Select it
            this.FontFamily = family;
            this.FontStyle = style;
            this.FontSizePt = size;
            this.FontSize = size/this.k;
            this.CurrentFont = this.fonts[fontkey];
            if( this.page > 0)
                this._out($"BT /F{this.CurrentFont.i} {this.FontSizePt:F2} Tf ET");
        }

        public void AddFont(string family, string style = "")
        {
            // Add a TrueType, OpenType or Type1 font
            family = family.ToLower();
            style = style.ToUpper();
            if(style == "IB")
                style = "BI";
            
            var fontkey = $"{family}{style}";
            if(!this.fonts.ContainsKey(fontkey))
                return;

            Font font = Font.GetDefaultFont();
            font.i = this.fonts.Count + 1;
            
            this.fonts[fontkey] = font;
        }
    }
}