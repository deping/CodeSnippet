#include <functional>

class ScopeExit
{
    using ExitFunc = std::function<void()>;
    ExitFunc _f;
public:
    ScopeExit(ExitFunc&& f) :
        _f(std::forward<ExitFunc>(f))
    {}

    ~ScopeExit()
    {
        _f();
    }
};

// PP_CONCAT is used to delay concatenation of se and __LINE__.
// or the result name is se__LINE__, not se123 ...
#define PP_CONCAT_IMPL(x, y) x##y
#define PP_CONCAT(x, y) PP_CONCAT_IMPL(x, y)
#define ON_EXIT(f) ScopeExit PP_CONCAT(se,__LINE__)(f)
