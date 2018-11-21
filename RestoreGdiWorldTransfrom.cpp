RestoreGdiWorldTransfrom::RestoreGdiWorldTransfrom()
    : m_modified(false)
{
}

void RestoreGdiWorldTransfrom::ModifyWorldTransform(HDC hDC, const XFORM& xform)
{
    m_modified = true;
    m_hDC = hDC;
    m_PrevGraphicsMode = GetGraphicsMode(hDC);
    GetWorldTransform(hDC, &m_PrevXform);
    if (m_PrevGraphicsMode == GM_ADVANCED)
    {
        //[x, y, 1] * XFORM
        ::ModifyWorldTransform(hDC, &xform, MWT_RIGHTMULTIPLY); //MWT_RIGHTMULTIPLY MWT_LEFTMULTIPLY
    }
    else
    {
        SetGraphicsMode(hDC, GM_ADVANCED);
        // The SetWorldTransform function will fail unless the graphics mode for the given device context
        // has been set to GM_ADVANCED by previously calling the SetGraphicsMode function.
        ::SetWorldTransform(hDC, &xform);
    }
}

RestoreGdiWorldTransfrom::~RestoreGdiWorldTransfrom()
{
    if (m_modified)
    {
        SetWorldTransform(m_hDC, &m_PrevXform);
        if (m_PrevGraphicsMode == GM_COMPATIBLE)
        {
            // It will not be possible to reset the graphics mode for the device context to the default GM_COMPATIBLE mode,
            // unless the world transformation has first been reset to the default identity transformation
            SetGraphicsMode(m_hDC, GM_COMPATIBLE);
        }
    }
}
