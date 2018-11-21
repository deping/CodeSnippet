class RestoreGdiWorldTransfrom
{
public:
    RestoreGdiWorldTransfrom();
    void ModifyWorldTransform(HDC hDC, const XFORM& xform);
    ~RestoreGdiWorldTransfrom();
private:
    bool m_modified;
    XFORM m_PrevXform;
    int m_PrevGraphicsMode;
    HDC m_hDC;
};
