#include <osg/ShapeDrawable>

class MyBBox : public osg::ShapeDrawable
{
public:
    MyBBox(osg::Drawable* drawable = nullptr);
    /** Copy constructor using CopyOp to manage deep vs shallow copy.*/
    MyBBox(const MyBBox& pg, const osg::CopyOp& copyop = osg::CopyOp::SHALLOW_COPY)
        : osg::ShapeDrawable(pg, copyop)
    {
        m_box = new osg::Box(*pg.m_box);
        setShape(m_box);
        m_object = pg.m_object;
    }

    virtual Object* cloneType() const { return new MyBBox(); }
    virtual Object* clone(const osg::CopyOp& copyop) const { return new MyBBox(*this, copyop); }
    virtual bool isSameKindAs(const Object* obj) const { return dynamic_cast<const MyBBox*>(obj) != NULL; }
    virtual const char* libraryName() const { return "osg"; }
    virtual const char* className() const { return "MyBBox"; }

    virtual void drawImplementation(osg::RenderInfo& renderInfo) const
    {
        osg::ShapeDrawable::drawImplementation(renderInfo);
    }
    osg::BoundingBox computeBoundingBox() const
    {
        return osg::ShapeDrawable::computeBoundingBox();
    }

    osg::ref_ptr<osg::Box> m_box;
    osg::ref_ptr<osg::Drawable> m_object;
};

class UpdateBBox : public osg::Drawable::UpdateCallback
{
public:
    virtual void update(osg::NodeVisitor*, osg::Drawable* drawable)
    {
        MyBBox* pBBox = dynamic_cast<MyBBox*>(drawable);
        if (pBBox)
        {
            auto bbox = pBBox->m_object->getBoundingBox();
            osg::Vec3 half(bbox.xMax() - bbox.xMin(), bbox.yMax() - bbox.yMin(), bbox.zMax() - bbox.zMin());
            half /= 2.0;
            pBBox->m_box->set(bbox.center(), half);
            pBBox->dirtyBound();
            pBBox->dirtyGLObjects();
            pBBox->build();
        }
    }
};

MyBBox::MyBBox(osg::Drawable* drawable)
{
    auto ss = getOrCreateStateSet();
    ss->setAttributeAndModes(new osg::PolygonMode(osg::PolygonMode::FRONT_AND_BACK, osg::PolygonMode::LINE));
    m_object = drawable;
    m_box = new osg::Box(osg::Vec3(), 1.0f);
    setShape(m_box.get());
    setUpdateCallback(new UpdateBBox);
}