#include <boost/throw_exception.hpp>

#include <exception>

#if defined(BOOST_NO_EXCEPTIONS)

// Boost declares these functions in this namespace when compiler exception support is disabled. MSVC builds without
// /EHsc hit that path, so provide terminating definitions for link compatibility.
namespace boost
{

void throw_exception(std::exception const& e)
{
    (void)e;
    std::terminate();
}

void throw_exception(std::exception const& e, boost::source_location const& loc)
{
    (void)e;
    (void)loc;
    std::terminate();
}

} // namespace boost

#endif
