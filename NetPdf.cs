using System.Collections;
using System.Reflection;
using System.Text;

namespace Tephanik
{
    public class NetPdf
    {
        private int page;               // current page number
        private int n;                  // current object number
        private int[] offsets;            // array of object offsets
        private StringBuilder buffer;             // buffer holding in-memory PDF
        private string[] pages;              // array containing pages
        // private List<string> pages;              // array containing pages
        private int state;              // current document state
        private bool compress;           // compression flag
        private double k;                  // scale factor (number of points in user unit)
        private string DefOrientation;     // default orientation
        private string CurOrientation;     // current orientation
        private dynamic StdPageSizes;       // standard page sizes
        private (double, double) DefPageSize;        // default page size
        private (double, double) CurPageSize;        // current page size
        private double CurRotation;        // current page rotation
        private Dictionary<string, dynamic>[] PageInfo;           // page-related data
        // private List<dynamic> PageInfo;           // page-related data
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
        private Dictionary<string, int> cmaps;              // array of ToUnicode CMaps
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
        private string NetPdfVersion = "1.0";
        public NetPdf(string orientation="P", string unit="mm", string inputSize="A4")
        {
            // Some checks
            // this._dochecks();
            // Initialization of properties
            this.state = 0;
            this.page = 0;
            this.n = 2;
            this.buffer = new StringBuilder();
            this.pages = new string[1000];
            this.PageInfo = new Dictionary<string, dynamic>[1000];
            for (var i = 0; i < this.PageInfo.Count(); i++)
            {
                this.PageInfo[i] = new Dictionary<string, dynamic>();
            }
            this.fonts =   new();
            this.FontFiles = new();
            this.encodings = new();
            this.cmaps = new();
            this.images = new();
            this.links = new();
            this.offsets = new int[1000];
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
            // if(defined("FPDF_FONTPATH"))
            // {
            //     this.fontpath = FPDF_FONTPATH;
            //     if(substr(this.fontpath,-1)!="/" && substr(this.fontpath,-1)!="\\")
            //         this.fontpath .= "/";
            // }
            // else if(is_dir(dirname(__FILE__)."/font"))
            //     this.fontpath = dirname(__FILE__)."/font/";
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

            // TODO: fix
            this.AddFont("Helvetica", "I");
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

        private void _newobj(int? _n = null)
        {
            // Begin a new object
            if(_n == null) {
                _n = ++this.n;
            } 
            this.offsets[(int) _n] = this._getoffset();
            this._put($"{_n} 0 obj");
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
            double _k = this.k;
            if(this.y + h > this.PageBreakTrigger && !this.InHeader && !this.InFooter && this.AcceptPageBreak())
            {
                // Automatic page break
                double _x = this.x;
                double _ws = this.ws;
                if(_ws > 0)
                {
                    this.ws = 0;
                    this._out("0 Tw");
                }
                this.AddPage(this.CurOrientation, this.CurPageSize, this.CurRotation);
                this.x = _x;
                if(_ws > 0) {
                    this.ws = _ws;
                    this._out($"{_ws*_k:F3} Tw");
                }
            }
            if(w == 0) {
                w = this.w - this.rMargin - this.x;
            }
            var s = "";
            var op = "";
            if(fill || border==1) {
                if(fill) {
                    op = (border==1) ? "B" : "f";
                } else {
                    op = "S";
                }
                s = $"{this.x*_k:F2} {(this.h-this.y)*_k:F2} {w*k:F2} {-h*k:F2} re {op} ";
            }
            // if(border is string)
            // {
            //     double _x = this.x;
            //     double _y = this.y;
            //     if(strpos(border,"L")!==false)
            //         s += sprintf("%.2F %.2F m %.2F %.2F l S ",x*k,(this.h-y)*k,x*k,(this.h-(y+h))*k);
            //     if(strpos(border,"T")!==false)
            //         s += sprintf("%.2F %.2F m %.2F %.2F l S ",x*k,(this.h-y)*k,(x+w)*k,(this.h-y)*k);
            //     if(strpos(border,"R")!==false)
            //         s += sprintf("%.2F %.2F m %.2F %.2F l S ",(x+w)*k,(this.h-y)*k,(x+w)*k,(this.h-(y+h))*k);
            //     if(strpos(border,"B")!==false)
            //         s += sprintf("%.2F %.2F m %.2F %.2F l S ",x*k,(this.h-(y+h))*k,(x+w)*k,(this.h-(y+h))*k);
            // }
            if(! string.IsNullOrEmpty(txt))
            {
                double? dx = null;
                if(this.CurrentFont.GetType() != typeof(Font))
                    this.Error("No font has been set");
                if(align == "R")
                    dx = w - this.cMargin-this.GetStringWidth(txt);
                else if(align == "C")
                    dx = (w-this.GetStringWidth(txt))/2;
                else
                    dx = this.cMargin;
                if(this.ColorFlag)
                    s += "q " + this.TextColor + " ";
                s += $"BT {(this.x+dx)*_k:F2} {(this.h-(this.y+.5*h+.3*this.FontSize))*_k:F2} Td ({this._escape(txt)}) Tj ET";
                // if(this.underline)
                //     s += " " . this._dounderline(this.x+dx,this.y+.5*h+.3*this.FontSize,txt);
                if(this.ColorFlag)
                    s += " Q";
                // if(link)
                //     this.Link(this.x+dx,this.y+.5*h-.5*this.FontSize,this.GetStringWidth(txt),this.FontSize,link);
            }
            if(! string.IsNullOrEmpty(s)) {
                this._out(s);
            }
            this.lasth = h;
            if(ln > 0)
            {
                // Go to next line
                this.y += h;
                if(ln == 1)
                    this.x = this.lMargin;
            }
            else
                this.x += w;
        }

        private string _escape(string s)
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

            return w*this.FontSize/1000;
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
                   orientation != this.CurOrientation 
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
                this.CurOrientation = orientation;
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
            var fontkey = $"{family}{style}".ToLower();
            if(! this.fonts.ContainsKey(fontkey))
            {
                // Test if one of the core fonts
                if(family == "arial")
                    family = "helvetica";
                if(this.CoreFonts.Contains(family))
                {
                    if(family=="symbol" || family=="zapfdingbats")
                        style = "";
                    fontkey = $"{family}{style}".ToLower();
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

        public void SetTextColor(double r, double? g = null, double? b = null)
        {
            // Set color for text
            if((r == 0 && g== 0 && b == 0) || g == null)
                this.TextColor = $"{r/255:F3} g";
            else
                this.TextColor = $"{r/255:F3} {g/255:F3} {b/255:F3} rg";
            this.ColorFlag = (this.FillColor != this.TextColor);
        }

        public void AddFont(string family, string style = "")
        {
            // Add a TrueType, OpenType or Type1 font
            family = family.ToLower();
            style = style.ToUpper();
            if(style == "IB")
                style = "BI";
            
            var fontkey = $"{family}{style}".ToLower();
            // if(!this.fonts.ContainsKey(fontkey))
            //     return;

            Font font = Font.GetDefaultFont();
            font.i = this.fonts.Count + 1;
            
            this.fonts[fontkey] = font;
        }

        public void Output(string dest = "", string name = "", bool isUTF8 = false)
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
                case "I":
                    // Send to standard output
                    // this._checkoutput();
                    // if(PHP_SAPI!="cli")
                    // {
                    //     // We send to a browser
                    //     header("Content-Type: application/pdf");
                    //     header("Content-Disposition: inline; ".this._httpencode("filename",$name,$isUTF8));
                    //     header("Cache-Control: private, max-age=0, must-revalidate");
                    //     header("Pragma: public");
                    // }
                    // echo this.buffer;
                    break;
                case "D":
                    // Download file
                    // this._checkoutput();
                    // header("Content-Type: application/x-download");
                    // header("Content-Disposition: attachment; ".this._httpencode("filename",$name,$isUTF8));
                    // header("Cache-Control: private, max-age=0, must-revalidate");
                    // header("Pragma: public");
                    // echo this.buffer;
                    break;
                case "F":
                    // Save to local file
                    try
                    {
                        var myByteArray = System.Text.Encoding.UTF8.GetBytes(this.buffer.ToString());
                        using(var f = new FileStream(name, FileMode.Create))
                        {
                            f.Write(myByteArray, 0, myByteArray.Length);
                        }
                        // var ms = new MemoryStream(myByteArray);
                        // File.WriteAllBytes(name, myByteArray);
                    }
                    catch (System.Exception)
                    {
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
            if(this.page == 0)
                this.AddPage();
            // Page footer
            this.InFooter = true;
            this.Footer();
            this.InFooter = false;
            // Close page
            this._endpage();
            // Close document
            this._enddoc();
        }

        protected void _putpage(int n)
        {
            this._newobj();
            this._put("<</Type /Page");
            this._put("/Parent 1 0 R");

            if(this.PageInfo[n].ContainsKey("size"))
                this._put($"/MediaBox [0 0 {this.PageInfo[n]["size"].Item1:F2} {this.PageInfo[n]["size"].Item2:F2}]");
            if(this.PageInfo[n].ContainsKey("rotation"))
                this._put("/Rotate " + (string) this.PageInfo[n]["rotation"]);
            this._put("/Resources 2 0 R");
            // if(isset(this.PageLinks[n]))
            // {
            //     // Links
            //     $annots = "/Annots [";
            //     foreach(this.PageLinks[$n] as $pl)
            //     {
            //         $rect = sprintf("%.2F %.2F %.2F %.2F",$pl[0],$pl[1],$pl[0]+$pl[2],$pl[1]-$pl[3]);
            //         $annots += "<</Type /Annot /Subtype /Link /Rect [".$rect."] /Border [0 0 0] ";
            //         if(is_string($pl[4]))
            //             $annots += "/A <</S /URI /URI ".this._textstring($pl[4]).">>>>";
            //         else
            //         {
            //             $l = this.links[$pl[4]];
            //             if(isset(this.PageInfo[$l[0]]["size"]))
            //                 $h = this.PageInfo[$l[0]]["size"][1];
            //             else
            //                 $h = (this.DefOrientation=="P") ? this.DefPageSize[1]*this.k : this.DefPageSize[0]*this.k;
            //             $annots += sprintf("/Dest [%d 0 R /XYZ 0 %.2F null]>>",this.PageInfo[$l[0]]["n"],$h-$l[1]*this.k);
            //         }
            //     }
            //     this._put($annots."]");
            // }
            if(this.WithAlpha)
                this._put("/Group <</Type /Group /S /Transparency /CS /DeviceRGB>>");
            this._put($"/Contents {this.n+1} 0 R>>");
            this._put("endobj");
            // Page content
            if(! string.IsNullOrEmpty(this.AliasNbPages))
                this.pages[n] = this.pages[n].Replace(this.AliasNbPages, $"{this.page}");
            this._putstreamobject(this.pages[n]);
        }

        protected void _putpages()
        {
            var nb = this.page;
            for(var _n = 1; _n <= nb; _n++)
                this.PageInfo[_n]["n"] = this.n+1+2*(_n-1);
            for(var n = 1; n <= nb; n++)
                this._putpage(n);
            // Pages root
            this._newobj(1);
            this._put("<</Type /Pages");
            var kids = "/Kids [";
            for(var _n=1; _n<= nb; _n++)
                kids += this.PageInfo[_n]["n"] + " 0 R ";
            this._put(kids + "]");
            this._put("/Count " + nb);
            if(this.DefOrientation=="P")
            {
                var w = this.DefPageSize.Item1;
                var h = this.DefPageSize.Item2;
            }
            else
            {
                var w = this.DefPageSize.Item2;
                var h = this.DefPageSize.Item1;
            }
            this._put($"/MediaBox [0 0 {w*this.k:F2} {h*this.k:F2}]");
            this._put(">>");
            this._put("endobj");
        }

        protected void _putheader()
        {
            this._put("%PDF-" + this.PDFVersion);
        }

        protected void _puttrailer()
        {
            this._put("/Size " + (this.n+1));
            this._put("/Root " + this.n + " 0 R");
            this._put("/Info " + (this.n-1) + " 0 R");
        }

        protected void _enddoc()
        {
            this._putheader();
            this._putpages();
            this._putresources();
            // Info
            this._newobj();
            this._put("<<");
            this._putinfo();
            this._put(">>");
            this._put("endobj");
            // Catalog
            this._newobj();
            this._put("<<");
            this._putcatalog();
            this._put(">>");
            this._put("endobj");
            // Cross-ref
            var offset = this._getoffset();
            this._put("xref");
            this._put("0 " + (this.n + 1));
            this._put("0000000000 65535 f ");
            for(var i = 1; i <= this.n; i++)
                this._put($"{this.offsets[i]:D10} 00000 n ");
            // Trailer
            this._put("trailer");
            this._put("<<");
            this._puttrailer();
            this._put(">>");
            this._put("startxref");
            this._put(offset.ToString());
            this._put("%%EOF");
            this.state = 3;
        }

        protected void _putxobjectdict()
        {
            // TODO: Implement this
            // foreach(var image in this.images)
            //     this._put("/I" + image["i"] + " " + image["n"] + " 0 R");
        }

        protected void _putresourcedict()
        {
            this._put("/ProcSet [/PDF /Text /ImageB /ImageC /ImageI]");
            this._put("/Font <<");
            foreach(var font in this.fonts) {
                var f = font.Value;
                this._put("/F" + f.i + " " + f.n + " 0 R");
            }
            this._put(">>");
            this._put("/XObject <<");
            this._putxobjectdict();
            this._put(">>");
        }

        protected void _putresources()
        {
            this._putfonts();
            // this._putimages();
            // Resource dictionary
            this._newobj(2);
            this._put("<<");
            this._putresourcedict();
            this._put(">>");
            this._put("endobj");
        }

        protected void _putinfo()
        {
            this.metadata["Producer"] = "NetPdf " + this.NetPdfVersion;
            this.metadata["CreationDate"] = $"D:{DateTime.Now:yyyyMMddHmmss}";
            foreach(var (key, value) in this.metadata)
                this._put("/"+ key + " "+ this._textstring(value));
        }

        protected string _textstring(string s)
        {
            // Format a text string
            // if(!this._isascii($s))
            //     $s = this._UTF8toUTF16($s);
            // return '('.this._escape($s).')';
            return $"({s})";
        }

        protected void _putfonts()
        {
            // foreach(this.FontFiles as $file=>$info)
            // {
            //     // Font file embedding
            //     this._newobj();
            //     this.FontFiles[$file]["n"] = this.n;
            //     $font = file_get_contents(this.fontpath.$file,true);
            //     if(!$font)
            //         this.Error("Font file not found: ".$file);
            //     $compressed = (substr($file,-2)==".z");
            //     if(!$compressed && isset($info["length2"]))
            //         $font = substr($font,6,$info["length1"]).substr($font,6+$info["length1"]+6,$info["length2"]);
            //     this._put("<</Length ".strlen($font));
            //     if($compressed)
            //         this._put("/Filter /FlateDecode");
            //     this._put("/Length1 ".$info["length1"]);
            //     if(isset($info["length2"]))
            //         this._put("/Length2 ".$info["length2"]." /Length3 0");
            //     this._put(">>");
            //     this._putstream($font);
            //     this._put("endobj");
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
                //         this._newobj();
                //         this._put("<</Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [".$font["diff"]."]>>");
                //         this._put("endobj");
                //         this.encodings[$font["enc"]] = this.n;
                //     }
                // }
                // ToUnicode CMap
                
                var cmapkey = font.enc;
                    
                if(! this.cmaps.ContainsKey(cmapkey))
                {
                    var cmap = this._tounicodecmap(font.uv);
                    this._putstreamobject(cmap);
                    this.cmaps[cmapkey] = this.n;
                }
                
                // Font object
                this.fonts[k].n = this.n+1;
                var type = font.type;
                var name = font.name;
                // if($font["subsetted"])
                //     $name = "AAAAAA+".$name;
                if(type == "Core")
                {
                    // Core font
                    this._newobj();
                    this._put("<</Type /Font");
                    this._put("/BaseFont /" + name);
                    this._put("/Subtype /Type1");
                    if(name != "Symbol" && name != "ZapfDingbats")
                        this._put("/Encoding /WinAnsiEncoding");
                    // if(isset($font["uv"]))
                        this._put("/ToUnicode " + this.cmaps[cmapkey] + " 0 R");
                    this._put(">>");
                    this._put("endobj");
                }
                // else if($type=="Type1" || $type=="TrueType")
                // {
                //     // Additional Type1 or TrueType/OpenType font
                //     this._newobj();
                //     this._put("<</Type /Font");
                //     this._put("/BaseFont /".$name);
                //     this._put("/Subtype /".$type);
                //     this._put("/FirstChar 32 /LastChar 255");
                //     this._put("/Widths ".(this.n+1)." 0 R");
                //     this._put("/FontDescriptor ".(this.n+2)." 0 R");
                //     if(isset($font["diff"]))
                //         this._put("/Encoding ".this.encodings[$font["enc"]]." 0 R");
                //     else
                //         this._put("/Encoding /WinAnsiEncoding");
                //     if(isset($font["uv"]))
                //         this._put("/ToUnicode ".this.cmaps[$cmapkey]." 0 R");
                //     this._put(">>");
                //     this._put("endobj");
                //     // Widths
                //     this._newobj();
                //     $cw = &$font["cw"];
                //     $s = "[";
                //     for($i=32;$i<=255;$i++)
                //         $s += $cw[chr($i)]." ";
                //     this._put($s."]");
                //     this._put("endobj");
                //     // Descriptor
                //     this._newobj();
                //     $s = "<</Type /FontDescriptor /FontName /".$name;
                //     foreach($font["desc"] as $k=>$v)
                //         $s += " /".$k." ".$v;
                //     if(!empty($font["file"]))
                //         $s += " /FontFile".($type=="Type1" ? "" : "2")." ".this.FontFiles[$font["file"]]["n"]." 0 R";
                //     this._put($s.">>");
                //     this._put("endobj");
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
        protected void _putcatalog()
        {
            var n = this.PageInfo[1]["n"];
            this._put("/Type /Catalog");
            this._put("/Pages 1 0 R");
            if(this.ZoomMode == "fullpage")
                this._put($"/OpenAction [{n} 0 R /Fit]");
            else if(this.ZoomMode == "fullwidth")
                this._put($"/OpenAction [{n} 0 R /FitH null]");
            else if(this.ZoomMode=="real")
                this._put($"/OpenAction [{n} 0 R /XYZ null null 1]");
            // else if(!is_string(this.ZoomMode))
            //     this._put($"/OpenAction [{n} 0 R /XYZ null null {this.ZoomMode/100:F2}]");
            if(this.LayoutMode=="single")
                this._put("/PageLayout /SinglePage");
            else if(this.LayoutMode=="continuous")
                this._put("/PageLayout /OneColumn");
            else if(this.LayoutMode=="two")
                this._put("/PageLayout /TwoColumnLeft");
        }

        protected void _putstream(string data)
        {
            this._put("stream");
            this._put(data);
            this._put("endstream");
        }

        protected void _putstreamobject(string data)
        {
            var entries = "";
            entries += "/Length " + data.Length;
            this._newobj();
            this._put("<<" + entries + ">>");
            this._putstream(data);
            this._put("endobj");
        }

        protected string _tounicodecmap(List<(int, dynamic)> uv)
        {
            var ranges = "";
            var nbr = 0;
            var chars = "";
            var nbc = 0;
            foreach(var (c, v) in uv)
            {
                var v_array = v as IEnumerable; 

                if (v is System.Runtime.CompilerServices.ITuple)
                {
                    ranges += $"<{c:X2}> <{c+v.Item2-1:X2}> <{v.Item1:X4}>\n";
                    nbr++;
                }
                else
                {
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
            if(nbr > 0)
            {
                s += $"{nbr} beginbfrange\n";
                s += ranges;
                s += "endbfrange\n";
            }
            if(nbc > 0)
            {
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