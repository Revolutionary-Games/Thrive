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

# install ninja build
RUN curl -L https://github.com/ninja-build/ninja/releases/download/v${NINJA_BUILD_VERSION}/ninja-linux.zip \
    -o /ninja.zip && unzip /ninja.zip -d /usr/bin/ && rm -f /ninja.zip && \
    chmod +x /usr/bin/ninja

# Seems like git is the easiest way to get the source code (no source tar with all files)
RUN git clone https://github.com/llvm/llvm-project.git --depth 1 \
    --branch llvmorg-${LLVM_VERSION} /llvm-project

RUN mkdir /llvm-binaries

# bootstrap build for clang that uses the llvm standard library in the second compilation stage
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
    # build fails due to non-relocatable .a files without this \
    -DLLVM_ENABLE_PIC=ON \
    # first stage must be installed system-wide for it to work \
    -DCMAKE_INSTALL_PREFIX=/ \
    -DCLANG_BOOTSTRAP_PASSTHROUGH="CMAKE_BUILD_TYPE;LLVM_PARALLEL_LINK_JOBS;LLVM_ENABLE_LLD;LLVM_LIBDIR_SUFFIX;COMPILER_RT_BUILD_CRT;LLVM_INCLUDE_TESTS;LLVM_BUILD_TESTS;LLVM_ENABLE_PIC;CMAKE_INSTALL_PREFIX" \
    -DBOOTSTRAP_LLVM_ENABLE_PROJECTS="clang;clang-tools-extra;libc;compiler-rt;lld;polly;pstl" \
    -DBOOTSTRAP_LLVM_ENABLE_RUNTIMES=all \
    -DBOOTSTRAP_LLVM_TARGETS_TO_BUILD="X86;WebAssembly" \
    # build now with the freshly built llvm libc runtime from the previous step \
    -DBOOTSTRAP_LLVM_ENABLE_LLVM_LIBC=ON -DBOOTSTRAP_LLVM_ENABLE_LIBCXX=ON \
    -DBOOTSTRAP_LIBCXX_USE_COMPILER_RT=ON -DBOOTSTRAP_LIBCXXABI_USE_LLVM_UNWINDER=ON \
    # make this clang compile using the clang libs by default (sadly these don't seem to work) \
    -DBOOTSTRAP_CLANG_DEFAULT_CXX_STDLIB=libc++ -DBOOTSTRAP_CLANG_DEFAULT_RTLIB=compiler-rt \
    -DBOOTSTRAP_CLANG_DEFAULT_LINKER=lld \
    # -DBOOTSTRAP_LLVM_DEFAULT_TARGET_TRIPLE=x86_64-unknown-linux-llvm && \
    # force on the llvm runtime use and fPIC as LLVM_ENABLE_PIC is not enough for some reason \
    # also need to specify the library path to load things from \
    -DBOOTSTRAP_CMAKE_CXX_FLAGS='--rtlib=compiler-rt -stdlib=libc++ -L/lib64/x86_64-unknown-linux-gnu -fPIC' \
    -DBOOTSTRAP_CMAKE_C_FLAGS='--rtlib=compiler-rt -fPIC' \
    -DBOOTSTRAP_CMAKE_SHARED_LINKER_FLAGS='-L/lib64/x86_64-unknown-linux-gnu' \
    # static libs don't use linker flags so don't specify those \
    -DBOOTSTRAP_CMAKE_EXE_LINKER_FLAGS='-L/lib64/x86_64-unknown-linux-gnu' && \
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

# # Second stage of the build, this time without any GCC parts to make
# # the final clang build that has a clean runtime library
# # This is really the third stage as the first container does 2 stages of clang building
# FROM almalinux:9 as llvm-stage2

# RUN dnf install -y --setopt=deltarpm=false \
#     git zlib-devel libxml2-devel libedit-devel ncurses-devel && dnf clean all

# ENV PARALLEL_LINK_JOBS "2"
# ENV CMAKE_VERSION "3.27.7"

# # Install cmake without installing gcc as a dependency
# RUN curl -L \
#     https://github.com/Kitware/CMake/releases/download/v${CMAKE_VERSION}/cmake-${CMAKE_VERSION}-linux-x86_64.tar.gz \
#     -o /cmake.tar.gz && tar xf /cmake.tar.gz && \
#     cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/bin /usr/ && \
#     cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/share /usr/ && rm -rf /cmake.tar.gz /cmake-*

# COPY --from=llvm-stage1 /llvm-project /llvm-project

# COPY --from=llvm-stage1 /usr/bin/ninja /usr/bin/ninja

# COPY --from=llvm-stage1 /llvm-binaries/include /usr/local/include/
# COPY --from=llvm-stage1 /llvm-binaries/lib64 /usr/local/lib64/
# COPY --from=llvm-stage1 /llvm-binaries/bin /usr/local/bin/
# COPY --from=llvm-stage1 /llvm-binaries/share /usr/local/share/
# COPY --from=llvm-stage1 /llvm-binaries/libexec /usr/local/libexec/

# RUN echo "/usr/local/lib64/x86_64-unknown-linux-gnu" > /etc/ld.so.conf.d/clang.conf
# RUN ldconfig

# RUN mkdir /llvm-binaries

# # Last step of the bootstrap process
# # bootstrap build for clang that uses the llvm standard library
# RUN mkdir /build && cd /build && cmake /llvm-project/llvm \
#     -DCMAKE_BUILD_TYPE=Release -DLLVM_LIBDIR_SUFFIX=64 \
#     -DCMAKE_CXX_COMPILER=clang++ -DCMAKE_C_COMPILER=clang \
#     -G "Ninja" -DLLVM_PARALLEL_LINK_JOBS=${PARALLEL_LINK_JOBS} -DLLVM_ENABLE_LLD=ON \
#     -DLLVM_ENABLE_RUNTIMES=all \
#     -DCOMPILER_RT_BUILD_CRT=ON -DLLVM_INCLUDE_TESTS=OFF -DLLVM_BUILD_TESTS=OFF \
#     -DLIBCXX_USE_COMPILER_RT=ON \
#     -DLLVM_ENABLE_PROJECTS="clang;clang-tools-extra;libc;compiler-rt;lld;polly;pstl" \
#     -DLLVM_TARGETS_TO_BUILD="X86;WebAssembly" \
#     -DLLVM_ENABLE_LLVM_LIBC=ON -DLLVM_ENABLE_LIBCXX=ON \
#     -DCMAKE_INSTALL_PREFIX=/llvm-binaries && \
#     # -DLLVM_DEFAULT_TARGET_TRIPLE=x86_64-unknown-linux-llvm && \
#     # -DBOOTSTRAP_CMAKE_CXX_FLAGS='--rtlib=compiler-rt' -DBOOTSTRAP_CMAKE_C_FLAGS='--rtlib=compiler-rt' && \
#     ninja install && \
#     rm -rf /build

# Final build image without temporary build artifacts and extra tools
FROM almalinux:9

# Apparently it is not possible to make a clang installation that
# doesn't require the following files from glibc-devel:
# Scrt1.o: No such file or directory
# crti.o: No such file or directory
# crtn.o: No such file or directory
# And the following files from gcc:
# crtbeginS.o: No such file or directory
# ld.lld: error: unable to find library -lgcc
# ld.lld: error: unable to find library -lgcc_s
# ld.lld: error: unable to find library -lc
# ld.lld: error: unable to find library -lgcc
# ld.lld: error: unable to find library -lgcc_s
# crtendS.o: No such file or directory

# glibc-headers would almost be enough but it is missing stubs-64.h
RUN dnf update -y && dnf install -y --setopt=deltarpm=false \
    make git binutils bash glibc-devel && dnf clean all

ENV CMAKE_VERSION "3.27.7"

# Install cmake without installing gcc as a dependency
RUN curl -L \
    https://github.com/Kitware/CMake/releases/download/v${CMAKE_VERSION}/cmake-${CMAKE_VERSION}-linux-x86_64.tar.gz \
    -o /cmake.tar.gz && tar xf /cmake.tar.gz && \
    cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/bin /usr/ && \
    cp -R /cmake-${CMAKE_VERSION}-linux-x86_64/share /usr/ && rm -rf /cmake.tar.gz /cmake-*

# Copy everything clang wants to install from the earlier build container
# COPY --from=llvm-builder /llvm-binaries/usr/include /usr/include/
# COPY --from=llvm-builder /llvm-binaries/include /include/
# COPY --from=llvm-builder /llvm-binaries/lib64 /usr/local/lib64/
# COPY --from=llvm-builder /llvm-binaries/usr/bin /usr/local/bin/
# COPY --from=llvm-builder /llvm-binaries/bin /usr/local/bin/
# COPY --from=llvm-builder /llvm-binaries/share /usr/local/share/
# COPY --from=llvm-builder /llvm-binaries/libexec /usr/local/libexec/
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

# RUN echo "export PATH=$PATH:/usr/local/bin" > /etc/profile.d/local_bin.sh

