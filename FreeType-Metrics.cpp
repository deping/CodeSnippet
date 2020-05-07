#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_GLYPH_H

class FreeType
{
    static inline void DoneFace(FT_Face face)
    {
        FT_Done_Face(face);
    }

public:
    FreeType()
        : m_library(nullptr), m_timer(0)
    {
        FT_Error error = FT_Init_FreeType(&m_library);
        if (error)
        {
        }
    }
    FT_Face NewFace(const CString &fontFile);
    void DoneFaces()
    {
        POSITION pos = m_faces.GetStartPosition();
        CString key;
        FT_Face face;
        while (pos != NULL)
        {
            m_faces.GetNextAssoc(pos, key, face);
            FreeType::DoneFace(face);
        }
        m_faces.RemoveAll();
        KillTimer(NULL, m_timer);
        m_timer = 0;
    }
    static inline void SetCharSize(FT_Face face,
                                   FT_F26Dot6 char_width,
                                   FT_F26Dot6 char_height,
                                   FT_UInt horz_resolution,
                                   FT_UInt vert_resolution)
    {
        FT_Set_Char_Size(face, char_width, char_height, horz_resolution, vert_resolution);
    }
    static inline void SetPixelSizes(FT_Face face,
                                     FT_UInt pixel_width,
                                     FT_UInt pixel_height)
    {
        FT_Set_Pixel_Sizes(face, pixel_width, pixel_height);
    }

    static inline void LoadChar(FT_Face face,
                                FT_ULong char_code,
                                FT_Int32 load_flags) // FT_LOAD_DEFAULT, FT_LOAD_COMPUTE_METRICS
    {
        FT_Load_Char(face, char_code, load_flags);
    }
    static inline FT_Glyph GetGlyph(FT_GlyphSlot glyph)
    {
        FT_Glyph res;
        FT_Get_Glyph(glyph, &res);
        return res;
    }
    static inline void DoneGlyph(FT_Glyph glyph)
    {
        FT_Done_Glyph(glyph);
    }
    ~FreeType()
    {
        DoneFaces();
        FT_Done_FreeType(m_library);
    }

    FT_Library m_library;
    CMap<CString, LPCTSTR, FT_Face, FT_Face> m_faces;
    UINT_PTR m_timer;
};

FreeType g_freetype;
void __stdcall DoneFacesTimerproc(
    HWND Arg1,
    UINT Arg2,
    UINT_PTR Arg3,
    DWORD Arg4)
{
    g_freetype.DoneFaces();
}
FT_Face FreeType::NewFace(const CString &fontFile)
{
    FT_Face face;
    BOOL found = m_faces.Lookup(fontFile, face);
    if (found)
        return face;
    LPITEMIDLIST ppidl;
    TCHAR lpsbuf[255];
    SHGetSpecialFolderLocation(NULL, CSIDL_FONTS, &ppidl);
    SHGetPathFromIDList(ppidl, lpsbuf);
    CoTaskMemFree(ppidl);
    const size_t len = _tcslen(lpsbuf);
    if (len >= sizeof(lpsbuf) - 1)
        // can't go on
        return nullptr;
    lpsbuf[len] = '\\';
    lpsbuf[len + 1] = '\0';
    FT_Error error = FT_New_Face(m_library,
                                 CT2A(CString(lpsbuf) + fontFile),
                                 0,
                                 &face);
    if (error == FT_Err_Cannot_Open_Resource)
    {
        // 试一试.ttc文件
        if (fontFile.Right(4).MakeLower() == _T(".ttf"))
        {
            CString fontFile2 = fontFile.Left(fontFile.GetLength() - 4) + _T(".ttc");
            error = FT_New_Face(m_library,
                                CT2A(CString(lpsbuf) + fontFile2),
                                0,
                                &face);
        }
    }
    if (error)
    {
        return nullptr;
    }
    m_faces[fontFile] = face;
    // 只要NewFace在超时时间内调用，计时器会被重置。
    SetTimer(NULL, m_timer, 5000, DoneFacesTimerproc);
    return face;
}

CDblPoint GetTextBaseLineLeft(const CDblPoint &position, UINT alignmode, double height, double Descent,
                              double width, double angle)
{
    //Don't change the order of comparison
    double detH = 0.0, detV = 0.0;
    if ((alignmode & TA_BOTTOM) == TA_BOTTOM)
    {
        detV = -Descent;
    }
    else if ((alignmode & TA_MIDDLE) == TA_MIDDLE)
    {
        detV = height / 2.0;
    }
    else if ((alignmode & TA_BOTTOM) != TA_BOTTOM) //TA_TOP
    {
        detV = height;
    }

    if ((alignmode & TA_CENTER) == TA_CENTER)
    {
        detH = width / 2.0;
    }
    else if ((alignmode & TA_RIGHT) == TA_RIGHT)
    {
        detH = width;
    }

    double sinangle = sin(angle);
    double cosangle = cos(angle);
    CDblPoint bottomleft;
    bottomleft.x = position.x - detH * cosangle + detV * sinangle;
    bottomleft.y = position.y - detH * sinangle - detV * cosangle;
    return bottomleft;
}

CDblPoint CDrawText::GetBaselineLeft()
{
    USES_CONVERSION;
    const TextStyleData *pTSD = m_pGraphBase->GetTextStyleData(m_StyleName);
    if (pTSD == NULL)
        return m_InsertionPoint;
    UINT trueAlignFlag = TextAlignTable[m_Alignment];
    double Ascent = 0, Descent = 0, width = 0;
    double textHeight = GetTextHeight();
    double widthFactor = GetWidthFactor();
    if (!pTSD->bIsShxFile)
    {
        FT_Face face = g_freetype.NewFace(pTSD->FontFile);
        if (face == nullptr)
            return CDblPoint();
        // 放大64倍
        auto fontHeight = FT_UInt(textHeight * 64.0);
        //const double* LookForWidthTableTtfFile(const CString& ttfFace);
        //const auto widthTable = LookForWidthTableTtfFile(pTSD->FontFile);
        double whratio;
        //if (widthTable)
        //	whratio = widthTable[96] / 100.0;
        //else
        whratio = 1.4382;
        auto fontWidth = FT_UInt(fontHeight * whratio); // 经验系数
        FreeType::SetPixelSizes(face, fontWidth, fontHeight);
        CStringW text(CT2W(m_Text).operator LPWSTR());
        size_t len = text.GetLength();
        int xmin = 0, ymin = 0, xmax = 0;
        for (size_t i = 0; i < len; ++i)
        {
            FreeType::LoadChar(face, text[i], FT_LOAD_COMPUTE_METRICS);
            FT_Glyph_Metrics metrics = face->glyph->metrics;
            if (i == 0)
            {
                xmin = 0; // metrics.horiBearingX;
                ymin = metrics.horiBearingY - metrics.height;
            }
            else
            {
                ymin = min(ymin, metrics.horiBearingY - metrics.height);
            }
            if (i == len - 1)
            {
                xmax += metrics.horiAdvance; // metrics.horiBearingX + metrics.width;
            }
            else
            {
                xmax += metrics.horiAdvance;
            }
        }
        // 为了提高程序性能，face被缓存。在空闲时用一个计时器来释放faces。
        //FreeType::DoneFace(face);
        // 缩小64倍
        // 再除以64，因为FT_Pos是26.6定点小数
        width = (xmax - xmin) / 64.0 / 64.0 * widthFactor;
        Descent = ymin / 64.0 / 64.0;
    }
    else
    {
        CRegBigFontShxParser parser(pTSD->FontFile, pTSD->bigFontFile);
        parser.SetTextHeight(textHeight);
        Ascent = textHeight;
        Descent = parser.GetDescendHeight();
        width = parser.GetTextExtent(m_Text) * widthFactor;
    }
    double angle = m_RotationAngle;
    CDblPoint baselineleft = GetTextBaseLineLeft(m_InsertionPoint, trueAlignFlag, textHeight, Descent, width, angle);
    return baselineleft;
}
