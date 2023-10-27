#pragma once

#include "boost/intrusive_ptr.hpp"

namespace Thrive
{
template<class T>
using Ref = boost::intrusive_ptr<T>;

template<class T>
using RefConst = boost::intrusive_ptr<const T>;

} // namespace Thrive
