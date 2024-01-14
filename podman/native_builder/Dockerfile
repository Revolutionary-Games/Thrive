# If the image version is updated here also Scripts/ContainerTool.cs needs to be updated
FROM almalinux:9 as llvm-builder

RUN dnf install -y --setopt=deltarpm=false \
    cmake gcc-c++ clang lld git unzip \
    zlib-devel libxml2-devel libedit-devel ncurses-devel && dnf clean all

ENV LLVM_VERSION "17.0.4"
ENV NINJA_BUILD_VERSION "1.11.1"

# Parallel link jobs is recommended to be available ram / 15 GB (so this is now optimized for
# 64 GB system)
ENV PARALLEL_LINK_JOBS "4"

# Install ninja build
RUN curl -L https://github.com/ninja-build/ninja/releases/download/v${NINJA_BUILD_VERSION}/ninja-linux.zip \
    -o /ninja.zip && unzip /ninja.zip -d /usr/bin/ && rm -f /ninja.zip && \
    chmod +x /usr/bin/ninja

# Seems like git is the easiest way to get the source code (no source tar with all files)
RUN git clone https://github.com/llvm/llvm-project.git --depth 1 \
    --branch llvmorg-${LLVM_VERSION} /llvm-project

RUN mkdir /llvm-binaries

# Bootstrap build for clang. It was attempted to use llvm standard
# library everywhere but that creates a library that segfaults on
# exit. For now only the C++ standard library is compiled, but even
# that isn't really used as it depends on non-standard libraries
# (atomic). This could be improved in the future. Glibc still needs to
# be dynamically linked (probably always) to ensure that things don't
# crash on shutdown on Linux (presumably due to Godot being compiled
# with glibc so having two different C libraries will cause an
# incompatibility)
RUN mkdir /build && cd /build && cmake /llvm-project/llvm -DCLANG_ENABLE_BOOTSTRAP=ON \
    # libdir suffix to match what redhat does \
    -DCMAKE_BUILD_TYPE=Release -DLLVM_LIBDIR_SUFFIX=64 \
    # first stage compiled with clang to avoid extra compiler errors \
    -DCMAKE_CXX_COMPILER=clang++ -DCMAKE_C_COMPILER=clang \
    -G "Ninja" -DLLVM_PARALLEL_LINK_JOBS=${PARALLEL_LINK_JOBS} -DLLVM_ENABLE_LLD=ON \
    -DLLVM_ENABLE_PROJECTS="clang;lld" \
    -DLLVM_ENABLE_RUNTIMES=all \
    -DLLVM_TARGETS_TO_BUILD="X86" \
    # configuring the built libc++ \
    -DLIBCXXABI_USE_LLVM_UNWINDER=ON -DLIBCXX_USE_COMPILER_RT=OFF \
    # build fails due to non-relocatable .a files without this \
    -DLLVM_ENABLE_PIC=ON \
    # first stage must be installed system-wide for it to work \
    -DCMAKE_INSTALL_PREFIX=/ \
    -DCLANG_BOOTSTRAP_PASSTHROUGH="CMAKE_BUILD_TYPE;LLVM_PARALLEL_LINK_JOBS;LLVM_ENABLE_LLD;LLVM_LIBDIR_SUFFIX;LIBCXXABI_USE_LLVM_UNWINDER;LIBCXX_USE_COMPILER_RT;LLVM_ENABLE_PIC;CMAKE_INSTALL_PREFIX" \
    -DBOOTSTRAP_LLVM_ENABLE_PROJECTS="clang;clang-tools-extra;libc;lld;polly;pstl" \
    -DBOOTSTRAP_LLVM_ENABLE_RUNTIMES=all \
    -DBOOTSTRAP_LLVM_TARGETS_TO_BUILD="X86;WebAssembly" \
    -DBOOTSTRAP_LLVM_ENABLE_LLVM_LIBC=OFF -DBOOTSTRAP_LLVM_ENABLE_LIBCXX=ON \
    # try to force linking against gcc_s off
    -DBOOTSTRAP_LIBCXX_HAS_GCC_S_LIB=OFF -DBOOTSTRAP_LIBCXX_HAS_ATOMIC_LIB=OFF \
    -DBOOTSTRAP_CLANG_DEFAULT_LINKER=lld \
    -DBOOTSTRAP_CMAKE_CXX_FLAGS='-fPIC' \
    -DBOOTSTRAP_CMAKE_C_FLAGS='-fPIC' \
    && \
    ninja install && \
    # need to make the system aware of the just installed new compiler \
    echo "/lib64/x86_64-unknown-linux-gnu" > /etc/ld.so.conf.d/clang.conf && ldconfig && \
    # Need to force the proper install folder again here otherwise the install location \
    # stays wrong \
    cmake /llvm-project/llvm -DCMAKE_INSTALL_PREFIX=/llvm-binaries \
    -DBOOTSTRAP_CMAKE_INSTALL_PREFIX=/llvm-binaries && \
    # now the second stage can be compiled with the libc runtime etc. use forced on and \
    # installed systemwide in the previous step \
    ninja stage2 && ninja stage2 install && \
    # verify the install location stuck correctly \
    [ -f /llvm-binaries/bin/clang ] && \
    echo "llvm was installed to the right folder" || \
    (echo "build failed / llvm was not installed to the desired folder"; exit 1) && \
    rm -rf /build

# pe-util tool builder
FROM almalinux:9 as pe-tools

RUN dnf install -y --setopt=deltarpm=false \
    git unzip xz gcc-c++ cmake && dnf clean all

RUN git clone https://github.com/gsauthof/pe-util.git /pe-util
RUN cd /pe-util && git submodule update --init && mkdir build && cd build && \
    cmake .. -DCMAKE_BUILD_TYPE=Release && make


# Cross compiler image
FROM almalinux:9 as llvm-cross

RUN dnf install -y --setopt=deltarpm=false \
    git unzip xz && dnf clean all

ENV CMAKE_VERSION "3.27.7"
ENV NINJA_BUILD_VERSION "1.11.1"

# llvm-mingw 20231031 with LLVM 17.0.4
ENV LLVM_MINGW_VERSION "20231031"

# This probably controls what runtime is used
# ucrt is much newer but only installed by default on Windows 10
# ENV LLVM_MINGW_TYPE "ucrt"

# msvcrt is older with fewer features but also runs by default on Windows 7
ENV LLVM_MINGW_TYPE "msvcrt"

# Install cmake without installing gcc as a dependency
RUN curl -L \
    https://github.com/Kitware/CMake/releases/download/v${CMAKE_VERSION}/cmake-${CMAKE_VERSION}-linux-x86_64.tar.gz \
    -o /cmake.tar.gz && tar xf /cmake.tar.gz && \
    cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/bin /usr/ && \
    cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/share /usr/ && rm -rf /cmake.tar.gz /cmake-*

# Install ninja build (for consistency with the non-cross build image)
RUN curl -L https://github.com/ninja-build/ninja/releases/download/v${NINJA_BUILD_VERSION}/ninja-linux.zip \
    -o /ninja.zip && unzip /ninja.zip -d /usr/bin/ && rm -f /ninja.zip && \
    chmod +x /usr/bin/ninja

# Use a binary release of LLVM-MINGW
RUN curl -L \
    https://github.com/mstorsjo/llvm-mingw/releases/download/${LLVM_MINGW_VERSION}/llvm-mingw-${LLVM_MINGW_VERSION}-${LLVM_MINGW_TYPE}-ubuntu-20.04-x86_64.tar.xz \
    -o /llvm-mingw.tar.gz && tar xf /llvm-mingw.tar.gz && \
    cp -R /llvm-mingw-${LLVM_MINGW_VERSION}-${LLVM_MINGW_TYPE}-ubuntu-20.04-x86_64/* /usr/ && \
    rm -rf /llvm-mingw.tar.gz /llvm-mingw-*

# RUN echo "export PATH=$PATH:/usr/local/bin" > /etc/profile.d/opt_bin.sh

# Copies last as these always re-run even with cache on
COPY --from=pe-tools /pe-util/build/peldd /usr/bin/

# Final build image without temporary build artifacts and extra tools
FROM almalinux:9

# gcc-c++ is needed on top of libstdc++-devel to get all standard headers
RUN dnf update -y && dnf install -y --setopt=deltarpm=false \
    git gcc-c++ glibc-devel && dnf clean all

# binutils is removed so that the default linker doesn't exist
# RUN dnf remove binutils -y
# Remove binutils but leave gcc
RUN rpm --erase binutils binutils-gold --nodeps

ENV CMAKE_VERSION "3.27.7"

# Install cmake without installing gcc as a dependency
RUN curl -L \
    https://github.com/Kitware/CMake/releases/download/v${CMAKE_VERSION}/cmake-${CMAKE_VERSION}-linux-x86_64.tar.gz \
    -o /cmake.tar.gz && tar xf /cmake.tar.gz && \
    cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/bin /usr/ && \
    cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/share /usr/ && rm -rf /cmake.tar.gz /cmake-*

COPY --from=llvm-builder /usr/bin/ninja /usr/bin/ninja

COPY --from=llvm-builder /llvm-binaries/usr/include /usr/include/
COPY --from=llvm-builder /llvm-binaries/include /include/
COPY --from=llvm-builder /llvm-binaries/lib64 /lib64/
COPY --from=llvm-builder /llvm-binaries/usr/bin /usr/bin/
COPY --from=llvm-builder /llvm-binaries/bin /bin/
COPY --from=llvm-builder /llvm-binaries/share /usr/share/
COPY --from=llvm-builder /llvm-binaries/libexec /usr/libexec/

# Setup default search path for it
RUN echo "/usr/lib64/x86_64-unknown-linux-gnu/" > /etc/ld.so.conf.d/clang.conf
RUN ldconfig

# When copied properly this exists thanks to the copy from the builder image
RUN [ -f /usr/bin/ld.lld ] && \
    echo "ld.lld created properly" || (echo "ld.lld was not installed"; exit 1)

RUN ln -s /usr/bin/ld.lld /usr/bin/ld


