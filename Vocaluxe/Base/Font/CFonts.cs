using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Font
{
    static class CFonts
    {
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings();

        private static List<SFont> _Fonts;
        private static int _CurrentFont;
        private static float _Height = 1f;

        public static int PartyModeID { get; set; }

        public static EStyle Style = EStyle.Normal;

        public static float Height
        {
            get { return _Height; }
            set { _Height = value < 0f ? 0f : value; }
        }

        public static float Outline
        {
            get { return _Fonts[_CurrentFont].Outline; }
        }

        public static SColorF OutlineColor
        {
            get { return _Fonts[_CurrentFont].OutlineColor; }
        }

        public static void Init()
        {
            _Settings.Indent = true;
            _Settings.Encoding = Encoding.UTF8;
            _Settings.ConformanceLevel = ConformanceLevel.Document;

            _CurrentFont = 0;
            PartyModeID = -1;
            BuildFonts();
        }

        public static void BuildFonts()
        {
            _Fonts = new List<SFont>();
            _CurrentFont = 0;

            _LoadFontList();
        }

        public static System.Drawing.Font GetFont()
        {
            return _GetCurrentFont().GetFont();
        }

        private static CFont _GetCurrentFont()
        {
            switch (Style)
            {
                case EStyle.Normal:
                    return _Fonts[_CurrentFont].Normal;
                case EStyle.Italic:
                    return _Fonts[_CurrentFont].Italic;
                case EStyle.Bold:
                    return _Fonts[_CurrentFont].Bold;
                case EStyle.BoldItalic:
                    return _Fonts[_CurrentFont].BoldItalic;
            }
            //Just in case...
            return _Fonts[_CurrentFont].Normal;
        }

        #region DrawText
        public static void DrawText(string text, int x, int y, int h)
        {
            DrawText(text, h, x, y, 0f, new SColorF(0f, 0f, 0f, 1f));
        }

        public static void DrawText(string text, float x, float y, float z)
        {
            DrawText(text, Height, x, y, z, new SColorF(0f, 0f, 0f, 1f));
        }

        public static void DrawText(string text, float h, float x, float y, float z, SColorF color)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;
            CFont font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in text)
            {
                font.DrawGlyph(chr, dx, y, Height, z, color);
                dx += font.GetWidth(chr);
            }
        }

        public static void DrawTextReflection(string text, float h, float x, float y, float z, SColorF color, float rspace, float rheight)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;
            CFont font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in text)
            {
                font.DrawGlyphReflection(chr, dx, y, Height, z, color, rspace, rheight);
                dx += font.GetWidth(chr);
            }
        }

        public static void DrawText(string text, float h, float x, float y, float z, SColorF color, float begin, float end)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;

            float dx = x;
            float w = GetTextWidth(text);
            if (w <= 0f)
                return;

            float x1 = x + w * begin;
            float x2 = x + w * end;

            CFont font = _GetCurrentFont();

            foreach (char chr in text)
            {
                float w2 = font.GetWidth(chr);
                float b = (x1 - dx) / w2;

                if (b < 1f)
                {
                    if (b < 0f)
                        b = 0f;
                    float e = (x2 - dx) / w2;
                    if (e > 0f)
                    {
                        if (e > 1f)
                            e = 1f;
                        font.DrawGlyph(chr, dx, y, Height, z, color, b, e);
                    }
                }
                dx += w2;
            }
        }
        #endregion DrawText

        public static FontStyle GetFontStyle()
        {
            switch (Style)
            {
                case EStyle.Normal:
                    return FontStyle.Regular;
                case EStyle.Italic:
                    return FontStyle.Italic;
                case EStyle.Bold:
                    return FontStyle.Bold;
                case EStyle.BoldItalic:
                    return FontStyle.Bold | FontStyle.Italic;
                default:
                    return FontStyle.Regular;
            }
        }

        public static void SetFont(string fontName)
        {
            int index = _GetPartyFontIndex(PartyModeID, fontName);
            if (index < 0)
                index = _GetThemeFontIndex(CConfig.Theme, fontName);
            if (index < 0)
                index = _GetFontIndex(fontName);
            if (index >= 0)
                _CurrentFont = index;
        }

        public static RectangleF GetTextBounds(CText text)
        {
            return GetTextBounds(text, text.Height);
        }

        public static RectangleF GetTextBounds(CText text, float height)
        {
            Height = height;
            return new RectangleF(text.X, text.Y, GetTextWidth(CLanguage.Translate(text.Text, text.TranslationID)),
                                  GetTextHeight(CLanguage.Translate(text.Text, text.TranslationID)));
        }

        public static float GetTextWidth(string text)
        {
            CFont font = _GetCurrentFont();
            return text.Sum(chr => font.GetWidth(chr));
        }

        public static float GetTextHeight(string text)
        {
            //return TextRenderer.MeasureText(text, GetFont()).Height;
            CFont font = _GetCurrentFont();
            return text == "" ? 0 : text.Select(font.GetHeight).Max();
        }

        private static void _LoadFontFiles(CXMLReader xmlReader, string fontFolder, string themeName = "", int partyModeId = -1)
        {
            string value = string.Empty;
            int i = 1;
            while (xmlReader.GetValue("//root/Fonts/Font" + i + "/Folder", out value, value))
            {
                SFont sf = new SFont {Folder = value, IsThemeFont = themeName != "", ThemeName = themeName, PartyModeID = partyModeId};

                bool ok = true;

                string name;
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/Name", out name, value);
                sf.Name = name;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileNormal", out value, value);
                sf.FileNormal = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                CFont f = new CFont(value);
                sf.Normal = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileItalic", out value, value);
                sf.FileItalic = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Italic = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileBold", out value, value);
                sf.FileBold = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Bold = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileBoldItalic", out value, value);
                sf.FileBoldItalic = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.BoldItalic = f;

                sf.Outline = 0f;
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/Outline", ref sf.Outline);

                sf.OutlineColor = new SColorF(0f, 0f, 0f, 1f);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorR", ref sf.OutlineColor.R);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorG", ref sf.OutlineColor.G);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorB", ref sf.OutlineColor.B);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorA", ref sf.OutlineColor.A);

                if (ok)
                    _Fonts.Add(sf);
                else
                {
                    string fontTypes;
                    if (partyModeId >= 0)
                        fontTypes = "theme fonts for party mode";
                    else if (themeName != "")
                        fontTypes = "theme fonts for theme \"" + themeName + "\"";
                    else
                        fontTypes = "basic fonts";
                    CLog.LogError("Error loading " + fontTypes + ": Error in Font" + i);
                }
                i++;
            }
        }

        /// <summary>
        ///     Load default fonts
        /// </summary>
        /// <returns></returns>
        private static bool _LoadFontList()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(CSettings.FolderFonts, CSettings.FileFonts));
            if (xmlReader == null)
                return false;

            _Fonts.Clear();

            _LoadFontFiles(xmlReader, Path.Combine(Directory.GetCurrentDirectory(), CSettings.FolderFonts));
            return true;
        }

        /// <summary>
        ///     Loads theme fonts from skin file
        /// </summary>
        public static void LoadThemeFonts(string themeName, string fontFolder, CXMLReader xmlReader)
        {
            _LoadFontFiles(xmlReader, fontFolder, themeName);
            CLog.StartBenchmark(1, "BuildGlyphs");
            _BuildGlyphs();
            CLog.StopBenchmark(1, "BuildGlyphs");
        }

        /// <summary>
        ///     Loads party mode fonts from skin file
        /// </summary>
        public static void LoadPartyModeFonts(int partyModeID, string fontFolder, CXMLReader xmlReader)
        {
            _LoadFontFiles(xmlReader, fontFolder, "", partyModeID);
            CLog.StartBenchmark(1, "BuildGlyphs");
            _BuildGlyphs();
            CLog.StopBenchmark(1, "BuildGlyphs");
        }

        public static void SaveThemeFonts(string themeName, XmlWriter writer)
        {
            if (_Fonts.Count == 0)
                return;

            int index = 0;
            int fontNr = 1;
            bool setStart = false;
            while (index < _Fonts.Count)
            {
                if (_Fonts[index].IsThemeFont && _Fonts[index].ThemeName == themeName)
                {
                    if (!setStart)
                    {
                        writer.WriteStartElement("Fonts");
                        setStart = true;
                    }

                    writer.WriteStartElement("Font" + fontNr);

                    writer.WriteElementString("Name", _Fonts[index].Name);
                    writer.WriteElementString("Folder", _Fonts[index].Folder);

                    writer.WriteElementString("Outline", _Fonts[index].Outline.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorR", _Fonts[index].OutlineColor.R.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorG", _Fonts[index].OutlineColor.G.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorB", _Fonts[index].OutlineColor.B.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorA", _Fonts[index].OutlineColor.A.ToString("#0.00"));

                    writer.WriteElementString("FileNormal", _Fonts[index].FileNormal);
                    writer.WriteElementString("FileBold", _Fonts[index].FileBold);
                    writer.WriteElementString("FileItalic", _Fonts[index].FileItalic);
                    writer.WriteElementString("FileBoldItalic", _Fonts[index].FileBoldItalic);

                    writer.WriteEndElement();

                    fontNr++;
                }
                index++;
            }

            if (setStart)
                writer.WriteEndElement();
        }

        public static void UnloadThemeFonts(string themeName)
        {
            if (_Fonts.Count == 0)
                return;

            int index = 0;
            while (index < _Fonts.Count)
            {
                if (_Fonts[index].IsThemeFont && _Fonts[index].ThemeName == themeName)
                {
                    _Fonts[index].Normal.UnloadAllGlyphs();
                    _Fonts[index].Italic.UnloadAllGlyphs();
                    _Fonts[index].Bold.UnloadAllGlyphs();
                    _Fonts[index].BoldItalic.UnloadAllGlyphs();
                    _Fonts.RemoveAt(index);
                }
                else
                    index++;
            }
        }

        private static int _GetFontIndex(string fontName)
        {
            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (!_Fonts[i].IsThemeFont && _Fonts[i].Name == fontName)
                    return i;
            }

            return -1;
        }

        private static int _GetThemeFontIndex(string themeName, string fontName)
        {
            if (themeName == "" || fontName == "")
                return -1;

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (_Fonts[i].IsThemeFont && _Fonts[i].Name == fontName && _Fonts[i].ThemeName == themeName)
                    return i;
            }

            return -1;
        }

        private static int _GetPartyFontIndex(int partyModeID, string fontName)
        {
            if (partyModeID == -1 || fontName == "")
                return -1;

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (!_Fonts[i].IsThemeFont && _Fonts[i].PartyModeID == partyModeID && _Fonts[i].Name == fontName)
                    return i;
            }

            return -1;
        }

        private static void _BuildGlyphs()
        {
            const string text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPGRSTUVWGXZ1234567890";

            for (int i = 0; i < _Fonts.Count; i++)
            {
                _CurrentFont = i;

                foreach (char chr in text)
                {
                    Style = EStyle.Normal;
                    _Fonts[_CurrentFont].Normal.AddGlyph(chr);
                    Style = EStyle.Bold;
                    _Fonts[_CurrentFont].Bold.AddGlyph(chr);
                    Style = EStyle.Italic;
                    _Fonts[_CurrentFont].Italic.AddGlyph(chr);
                    Style = EStyle.BoldItalic;
                    _Fonts[_CurrentFont].BoldItalic.AddGlyph(chr);
                }
            }
            Style = EStyle.Normal;
            SetFont("Normal");
        }
    }
}