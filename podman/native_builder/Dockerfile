# If the image version is updated here also Scripts/ContainerTool.cs needs to be updated
FROM almalinux:9 as llvmbuilder

# cmake here pulls in gcc...
RUN dnf install -y --setopt=deltarpm=false \
    cmake make clang lld git unzip && dnf clean all

ENV LLVM_VERSION "17.0.4"
ENV NINJA_BUILD_VERSION "1.11.1"

# Parallel link jobs is recommended to be available ram / 15 GB (so this is now optimized for
# 32 GB system)
ENV PARALLEL_LINK_JOBS "2"

# install ninja build
RUN curl -L https://github.com/ninja-build/ninja/releases/download/v${NINJA_BUILD_VERSION}/ninja-linux.zip \
    -o /ninja.zip && unzip /ninja.zip -d /usr/bin/ && rm -f /ninja.zip && chmod +x /usr/bin/ninja

# Seems like git is the easiest way to get the source code (no source tar with all files)
RUN git clone https://github.com/llvm/llvm-project.git --depth 1 --branch llvmorg-${LLVM_VERSION} /llvm-project

RUN mkdir /llvm-binaries

RUN mkdir /build && cd /build && cmake /llvm-project/llvm -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_CXX_COMPILER=clang++ -DCMAKE_C_COMPILER=clang -G "Ninja" \
    -DLLVM_PARALLEL_LINK_JOBS=${PARALLEL_LINK_JOBS} -DLLVM_ENABLE_RUNTIMES=all \
    -DLLVM_ENABLE_PROJECTS="clang;clang-tools-extra;libc;lld;polly;pstl" -DLLVM_LIBDIR_SUFFIX=64 \
    -DLLVM_TARGETS_TO_BUILD="X86;WebAssembly" -DLLVM_USE_LINKER=lld \
    -DCMAKE_INSTALL_PREFIX=/llvm-binaries && cmake --build . -j --target install && \
    rm -rf /build

# Final build image without temporary build artifacts
FROM almalinux:9

RUN dnf update -y && dnf install -y --setopt=deltarpm=false \
    make git binutils bash && dnf clean all

ENV CMAKE_VERSION "3.27.7"

# Copy everything clang wants to install
COPY --from=llvmbuilder /llvm-binaries/include /usr/include
COPY --from=llvmbuilder /llvm-binaries/lib64 /lib64
COPY --from=llvmbuilder /llvm-binaries/bin /usr/bin
COPY --from=llvmbuilder /llvm-binaries/share /usr/share
COPY --from=llvmbuilder /llvm-binaries/libexec /usr/libexec

# Install cmake without installing gcc as a dependency
RUN curl -L \
    https://github.com/Kitware/CMake/releases/download/v${CMAKE_VERSION}/cmake-${CMAKE_VERSION}-linux-x86_64.sh \
    -o /cmake.sh && sh /cmake.sh --skip-license && rm -f /cmake.sh
