#pragma once

template<std::size_t... Indices>
struct indices 
{
};

template<std::size_t N, std::size_t... Indices>
struct build_indices : build_indices<N-1, N-1, Indices...>
{
};

template<std::size_t... Indices>
struct build_indices<0, Indices...> : indices<Indices...>
{
}

template<typename Tuple>
using IndicesFor = build_indices<std::tuple_size<Tuple>::value>;

