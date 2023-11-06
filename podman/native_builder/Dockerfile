# If the image version is updated here also Scripts/ContainerTool.cs needs to be updated
FROM almalinux:9 as llvm-builder

RUN dnf install -y --setopt=deltarpm=false \
    cmake gcc-c++ clang compiler-rt lld git unzip \
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

# Bootstrap build for clang that uses the llvm standard library in the second compilation stage
RUN mkdir /build && cd /build && cmake /llvm-project/llvm -DCLANG_ENABLE_BOOTSTRAP=ON \
    # libdir suffix to match what redhat does \
    -DCMAKE_BUILD_TYPE=Release -DLLVM_LIBDIR_SUFFIX=64 \
    # first stage compiled with clang to avoid extra compiler errors \
    -DCMAKE_CXX_COMPILER=clang++ -DCMAKE_C_COMPILER=clang \
    -G "Ninja" -DLLVM_PARALLEL_LINK_JOBS=${PARALLEL_LINK_JOBS} -DLLVM_ENABLE_LLD=ON \
    -DLLVM_ENABLE_PROJECTS="clang;libc;compiler-rt;lld" \
    -DLLVM_ENABLE_RUNTIMES=all \
    -DLLVM_TARGETS_TO_BUILD="X86" \
    -DCOMPILER_RT_BUILD_CRT=ON -DLLVM_INCLUDE_TESTS=OFF -DLLVM_BUILD_TESTS=OFF \
    # make files made by the first stage compiler use compiler-rt \
    -DCLANG_DEFAULT_RTLIB=compiler-rt \
    # try to make sure that the built libc++ doesn't depend on gcc \
    -DLIBCXXABI_USE_LLVM_UNWINDER=ON -DLIBCXX_USE_COMPILER_RT=ON \
    # trying to remove dependency on atomic lib \
    -DCOMPILER_RT_BUILD_BUILTINS=ON \
    # this causes build failures if turned on \
    # -DCOMPILER_RT_BUILD_STANDALONE_LIBATOMIC=ON \
    # build fails due to non-relocatable .a files without this \
    -DLLVM_ENABLE_PIC=ON \
    # first stage must be installed system-wide for it to work \
    -DCMAKE_INSTALL_PREFIX=/ \
    -DCLANG_BOOTSTRAP_PASSTHROUGH="CMAKE_BUILD_TYPE;LLVM_PARALLEL_LINK_JOBS;LLVM_ENABLE_LLD;LLVM_LIBDIR_SUFFIX;COMPILER_RT_BUILD_CRT;LLVM_INCLUDE_TESTS;LLVM_BUILD_TESTS;CLANG_DEFAULT_RTLIB;LIBCXXABI_USE_LLVM_UNWINDER;LIBCXX_USE_COMPILER_RT;COMPILER_RT_BUILD_BUILTINS;COMPILER_RT_BUILD_STANDALONE_LIBATOMIC;LLVM_ENABLE_PIC;CMAKE_INSTALL_PREFIX" \
    -DBOOTSTRAP_LLVM_ENABLE_PROJECTS="clang;clang-tools-extra;libc;compiler-rt;lld;polly;pstl" \
    -DBOOTSTRAP_LLVM_ENABLE_RUNTIMES=all \
    -DBOOTSTRAP_LLVM_TARGETS_TO_BUILD="X86;WebAssembly" \
    # build now with the freshly built llvm libc runtime from the previous step \
    -DBOOTSTRAP_LLVM_ENABLE_LLVM_LIBC=ON -DBOOTSTRAP_LLVM_ENABLE_LIBCXX=ON \
    # try to force linking against gcc_s off
    -DBOOTSTRAP_LIBCXX_HAS_GCC_S_LIB=OFF -DBOOTSTRAP_LIBCXX_HAS_ATOMIC_LIB=OFF \
    # make this clang compile using the clang libs by default (sadly these don't seem to work) \
    -DBOOTSTRAP_CLANG_DEFAULT_CXX_STDLIB=libc++ -DBOOTSTRAP_CLANG_DEFAULT_LINKER=lld \
    # -DBOOTSTRAP_LLVM_DEFAULT_TARGET_TRIPLE=x86_64-unknown-linux-llvm && \
    # force on the llvm runtime use and fPIC as LLVM_ENABLE_PIC is not enough for some reason \
    # also need to specify the library path to load things from \
    -DBOOTSTRAP_CMAKE_CXX_FLAGS='--rtlib=compiler-rt -stdlib=libc++ -L/lib64/x86_64-unknown-linux-gnu -fPIC' \
    -DBOOTSTRAP_CMAKE_C_FLAGS='--rtlib=compiler-rt -fPIC' \
    -DBOOTSTRAP_CMAKE_SHARED_LINKER_FLAGS='-L/lib64/x86_64-unknown-linux-gnu' \
    # static libs don't use linker flags so don't specify those \
    -DBOOTSTRAP_CMAKE_EXE_LINKER_FLAGS='--rtlib=compiler-rt -stdlib=libc++ -L/lib64/x86_64-unknown-linux-gnu' && \
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
    [ -f /llvm-binaries/lib64/x86_64-unknown-linux-gnu/libllvmlibc.a ] && \
    echo "libllvmlibc was installed correctly" || \
    (echo "build failed / libllvmlibc was not installed"; exit 1) && \
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

# glibc-headers would almost be enough but it is missing stubs-64.h
RUN dnf update -y && dnf install -y --setopt=deltarpm=false \
    make git binutils bash glibc-devel && dnf clean all

# binutils is removed so that the default linker doesn't exist
RUN dnf remove binutils -y

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
# RUN echo "/usr/local/bin" > /etc/ld.so.conf.d/local.conf
RUN ldconfig

# When copied properly this exists thanks to the copy from the builder image
RUN [ -f /usr/bin/ld.lld ] && \
    echo "ld.lld created properly" || (echo "ld.lld was not installed"; exit 1)

RUN ln -s /usr/bin/ld.lld /usr/bin/ld

# RUN echo "export PATH=$PATH:/usr/local/bin" > /etc/profile.d/local_bin.sh

