namespace Tephanik
{
    public class Font
    {
        public string? fontkey { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public int up { get; set; }
        public int ut { get; set; }
        public List<(char, int)> cw { get; set; }
        public string? enc { get; set; }
        public List<(int, dynamic)> uv { get; set; }

        public double? i { get; set; }

        public static Font GetDefaultFont() {
            return new Font {
                fontkey = "helveticai",
                type = "Core",
                name = "Helvetica-Oblique",
                up = -100,
                ut = 50,
                cw = new List<(char, int)> {
                    (Convert.ToChar(0), 278),(Convert.ToChar(1), 278),(Convert.ToChar(2), 278),(Convert.ToChar(3), 278),(Convert.ToChar(4), 278),(Convert.ToChar(5), 278),(Convert.ToChar(6), 278),(Convert.ToChar(7), 278),(Convert.ToChar(8), 278),(Convert.ToChar(9), 278),(Convert.ToChar(10), 278),(Convert.ToChar(11), 278),(Convert.ToChar(12), 278),(Convert.ToChar(13), 278),(Convert.ToChar(14), 278),(Convert.ToChar(15), 278),(Convert.ToChar(16), 278),(Convert.ToChar(17), 278),(Convert.ToChar(18), 278),(Convert.ToChar(19), 278),(Convert.ToChar(20), 278),(Convert.ToChar(21), 278),
                    (Convert.ToChar(22), 278),(Convert.ToChar(23), 278),(Convert.ToChar(24), 278),(Convert.ToChar(25), 278),(Convert.ToChar(26), 278),(Convert.ToChar(27), 278),(Convert.ToChar(28), 278),(Convert.ToChar(29), 278),(Convert.ToChar(30), 278),(Convert.ToChar(31), 278),(' ',278),('!',278),('"',355),('#',556),('$',556),('%',889),('&',667),('\'',191),('(',333),(')',333),('*',389),('+',584),
                    (',',278),('-',333),('.',278),('/',278),('0',556),('1',556),('2',556),('3',556),('4',556),('5',556),('6',556),('7',556),('8',556),('9',556),(':',278),(';',278),('<',584),('=',584),('>',584),('?',556),('@',1015),('A',667),
                    ('B',667),('C',722),('D',722),('E',667),('F',611),('G',778),('H',722),('I',278),('J',500),('K',667),('L',556),('M',833),('N',722),('O',778),('P',667),('Q',778),('R',722),('S',667),('T',611),('U',722),('V',667),('W',944),
                    ('X',667),('Y',667),('Z',611),('[',278),('\\',278),(']',278),('^',469),('_',556),('`',333),('a',556),('b',556),('c',500),('d',556),('e',556),('f',278),('g',556),('h',556),('i',222),('j',222),('k',500),('l',222),('m',833),
                    ('n',556),('o',556),('p',556),('q',556),('r',333),('s',500),('t',278),('u',556),('v',500),('w',722),('x',500),('y',500),('z',500),('{',334),('|',260),('}',334),('~',584),(Convert.ToChar(127), 350),(Convert.ToChar(128), 556),(Convert.ToChar(129), 350),(Convert.ToChar(130), 222),(Convert.ToChar(131), 556),
                    (Convert.ToChar(132), 333),(Convert.ToChar(133), 1000),(Convert.ToChar(134), 556),(Convert.ToChar(135), 556),(Convert.ToChar(136), 333),(Convert.ToChar(137), 1000),(Convert.ToChar(138), 667),(Convert.ToChar(139), 333),(Convert.ToChar(140), 1000),(Convert.ToChar(141), 350),(Convert.ToChar(142), 611),(Convert.ToChar(143), 350),(Convert.ToChar(144), 350),(Convert.ToChar(145), 222),(Convert.ToChar(146), 222),(Convert.ToChar(147), 333),(Convert.ToChar(148), 333),(Convert.ToChar(149), 350),(Convert.ToChar(150), 556),(Convert.ToChar(151), 1000),(Convert.ToChar(152), 333),(Convert.ToChar(153), 1000),
                    (Convert.ToChar(154), 500),(Convert.ToChar(155), 333),(Convert.ToChar(156), 944),(Convert.ToChar(157), 350),(Convert.ToChar(158), 500),(Convert.ToChar(159), 667),(Convert.ToChar(160), 278),(Convert.ToChar(161), 333),(Convert.ToChar(162), 556),(Convert.ToChar(163), 556),(Convert.ToChar(164), 556),(Convert.ToChar(165), 556),(Convert.ToChar(166), 260),(Convert.ToChar(167), 556),(Convert.ToChar(168), 333),(Convert.ToChar(169), 737),(Convert.ToChar(170), 370),(Convert.ToChar(171), 556),(Convert.ToChar(172), 584),(Convert.ToChar(173), 333),(Convert.ToChar(174), 737),(Convert.ToChar(175), 333),
                    (Convert.ToChar(176), 400),(Convert.ToChar(177), 584),(Convert.ToChar(178), 333),(Convert.ToChar(179), 333),(Convert.ToChar(180), 333),(Convert.ToChar(181), 556),(Convert.ToChar(182), 537),(Convert.ToChar(183), 278),(Convert.ToChar(184), 333),(Convert.ToChar(185), 333),(Convert.ToChar(186), 365),(Convert.ToChar(187), 556),(Convert.ToChar(188), 834),(Convert.ToChar(189), 834),(Convert.ToChar(190), 834),(Convert.ToChar(191), 611),(Convert.ToChar(192), 667),(Convert.ToChar(193), 667),(Convert.ToChar(194), 667),(Convert.ToChar(195), 667),(Convert.ToChar(196), 667),(Convert.ToChar(197), 667),
                    (Convert.ToChar(198), 1000),(Convert.ToChar(199), 722),(Convert.ToChar(200), 667),(Convert.ToChar(201), 667),(Convert.ToChar(202), 667),(Convert.ToChar(203), 667),(Convert.ToChar(204), 278),(Convert.ToChar(205), 278),(Convert.ToChar(206), 278),(Convert.ToChar(207), 278),(Convert.ToChar(208), 722),(Convert.ToChar(209), 722),(Convert.ToChar(210), 778),(Convert.ToChar(211), 778),(Convert.ToChar(212), 778),(Convert.ToChar(213), 778),(Convert.ToChar(214), 778),(Convert.ToChar(215), 584),(Convert.ToChar(216), 778),(Convert.ToChar(217), 722),(Convert.ToChar(218), 722),(Convert.ToChar(219), 722),
                    (Convert.ToChar(220), 722),(Convert.ToChar(221), 667),(Convert.ToChar(222), 667),(Convert.ToChar(223), 611),(Convert.ToChar(224), 556),(Convert.ToChar(225), 556),(Convert.ToChar(226), 556),(Convert.ToChar(227), 556),(Convert.ToChar(228), 556),(Convert.ToChar(229), 556),(Convert.ToChar(230), 889),(Convert.ToChar(231), 500),(Convert.ToChar(232), 556),(Convert.ToChar(233), 556),(Convert.ToChar(234), 556),(Convert.ToChar(235), 556),(Convert.ToChar(236), 278),(Convert.ToChar(237), 278),(Convert.ToChar(238), 278),(Convert.ToChar(239), 278),(Convert.ToChar(240), 556),(Convert.ToChar(241), 556),
                    (Convert.ToChar(242), 556),(Convert.ToChar(243), 556),(Convert.ToChar(244), 556),(Convert.ToChar(245), 556),(Convert.ToChar(246), 556),(Convert.ToChar(247), 584),(Convert.ToChar(248), 611),(Convert.ToChar(249), 556),(Convert.ToChar(250), 556),(Convert.ToChar(251), 556),(Convert.ToChar(252), 556),(Convert.ToChar(253), 500),(Convert.ToChar(254), 556),(Convert.ToChar(255), 500)
                },
                enc = "cp1252",
                uv = new List<(int, dynamic)> {
                    (0, (0,128)), (128,8364),(130,8218),(131,402),(132,8222),(133,8230),(134,(8224,2)),(136,710),(137,8240),(138,352),(139,8249),(140,338),(142,381),(145,(8216,2)),(147,(8220,2)),(149,8226),(150,(8211,2)),(152,732),(153,8482),(154,353),(155,8250),(156,339),(158,382),(159,376),(160,(160,96))
                } 
            };
        }
    }
}